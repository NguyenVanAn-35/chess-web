using MajestyGambit.Web.Data;
using MajestyGambit.Web.Entities;
using MajestyGambit.Web.Models.Game;
using MajestyGambit.Web.Services.Chess;
using Microsoft.EntityFrameworkCore;

namespace MajestyGambit.Web.Services;

public class GameService(AppDbContext db)
{
    private const int EloK = 20;

    public async Task CheckTimeoutsAsync()
    {
        var activeIds = await db.Games.Where(g => g.Result == null).Select(g => g.Id).ToListAsync();
        foreach (var id in activeIds)
        {
            try { await ProcessTimeoutAsync(id, force: false); }
            catch { /* ignore per-game errors */ }
        }
    }

    public async Task<ChessGameState?> ProcessTimeoutAsync(long gameId, bool force)
    {
        var game = await LoadGameAsync(gameId);
        if (game == null || game.Result != null) return null;

        var engine = SimpleChessEngine.FromFen(game.CurrentFen);
        var (whiteMs, blackMs) = GetLiveClock(game, engine.WhiteToMove);
        var remaining = engine.WhiteToMove ? whiteMs : blackMs;

        if (!force && remaining > 500) return null;

        game.WhiteTimeMs = whiteMs;
        game.BlackTimeMs = blackMs;
        game.Result = engine.WhiteToMove ? GameResult.black_win : GameResult.white_win;
        game.WinnerColor = engine.WhiteToMove ? WinnerColor.black : WinnerColor.white;
        game.EndReason = EndReason.timeout;
        game.EndedAt = DateTime.UtcNow;
        MaybeUpdateRatings(game);
        await db.SaveChangesAsync();
        return BuildState(game);
    }

    public async Task<Game> StartGameAsync(Room room)
    {
        var players = await db.RoomPlayers.Include(p => p.User)
            .Where(p => p.RoomId == room.Id).ToListAsync();
        if (players.Count < 2) throw new InvalidOperationException("Cần đủ 2 người chơi");

        RoomPlayer whiteRp, blackRp;
        var hostPref = room.HostColorPreference;
        if (hostPref is "white" or "black")
        {
            var hostRp = players.FirstOrDefault(p => p.UserId == room.HostId) ?? players[0];
            var guestRp = players.First(p => p.UserId != hostRp.UserId);
            if (hostPref == "white")
            {
                hostRp.Color = PieceColor.white; guestRp.Color = PieceColor.black;
                whiteRp = hostRp; blackRp = guestRp;
            }
            else
            {
                hostRp.Color = PieceColor.black; guestRp.Color = PieceColor.white;
                whiteRp = guestRp; blackRp = hostRp;
            }
        }
        else
        {
            players = players.OrderBy(_ => Random.Shared.Next()).ToList();
            players[0].Color = PieceColor.white;
            players[1].Color = PieceColor.black;
            whiteRp = players[0];
            blackRp = players[1];
        }

        room.Status = RoomStatus.in_progress;
        room.StartedAt = DateTime.UtcNow;
        var timeMs = (long)room.TimeLimitSeconds * 1000L;
        var game = new Game
        {
            RoomId = room.Id,
            WhitePlayerId = whiteRp.UserId,
            BlackPlayerId = blackRp.UserId,
            WhiteTimeMs = timeMs,
            BlackTimeMs = timeMs,
            CurrentFen = Game.DefaultFen
        };
        db.Games.Add(game);
        await db.SaveChangesAsync();

        await db.Entry(game).Reference(g => g.WhitePlayer).LoadAsync();
        await db.Entry(game).Reference(g => g.BlackPlayer).LoadAsync();
        await db.Entry(game).Reference(g => g.Room).LoadAsync();
        return game;
    }

    public async Task<ChessGameState> ProcessMoveAsync(long gameId, string fromSq, string toSq, string? promotion, string username)
    {
        var game = await LoadGameAsync(gameId) ?? throw new InvalidOperationException("Game không tồn tại");
        if (game.Result != null) throw new InvalidOperationException("Game đã kết thúc");

        var engine = SimpleChessEngine.FromFen(game.CurrentFen);
        var isWhiteTurn = engine.WhiteToMove;
        var expected = isWhiteTurn ? game.WhitePlayer.Username : game.BlackPlayer.Username;
        if (!string.Equals(expected, username, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Không phải lượt của bạn");

        var now = DateTime.UtcNow;
        var reference = game.LastMoveAt ?? game.StartedAt;
        var elapsedMs = (long)Math.Max(0, (now - reference).TotalMilliseconds);
        var remaining = isWhiteTurn ? game.WhiteTimeMs - elapsedMs : game.BlackTimeMs - elapsedMs;
        if (isWhiteTurn) game.WhiteTimeMs = Math.Max(0, remaining);
        else game.BlackTimeMs = Math.Max(0, remaining);
        game.LastMoveAt = now;

        if (remaining <= 0)
        {
            game.Result = isWhiteTurn ? GameResult.black_win : GameResult.white_win;
            game.WinnerColor = isWhiteTurn ? WinnerColor.black : WinnerColor.white;
            game.EndReason = EndReason.timeout;
            game.EndedAt = now;
            MaybeUpdateRatings(game);
            await db.SaveChangesAsync();
            return BuildState(game);
        }

        if (!engine.TryMove(fromSq, toSq, promotion, out var piece, out var capture, out var check, out var mate, out var stale))
            throw new InvalidOperationException("Nước đi không hợp lệ");

        var newFen = engine.ToFen();
        var moveNum = await db.Moves.CountAsync(m => m.GameId == game.Id) + 1;
        var san = $"{piece}: {fromSq.ToLowerInvariant()}{(capture ? "x" : "")}{toSq.ToLowerInvariant()}" +
                  (string.IsNullOrEmpty(promotion) ? "" : $"={promotion.ToUpperInvariant()}") +
                  (mate ? "#" : check ? "+" : "");

        db.Moves.Add(new Move
        {
            GameId = game.Id,
            MoveNumber = moveNum,
            PlayerColor = isWhiteTurn ? PieceColor.white : PieceColor.black,
            FromSquare = fromSq.ToLowerInvariant(),
            ToSquare = toSq.ToLowerInvariant(),
            Piece = piece,
            Promotion = promotion,
            San = san,
            FenAfter = newFen,
            TimeSpentMs = (int)elapsedMs
        });

        game.CurrentFen = newFen;
        if (mate)
        {
            game.Result = isWhiteTurn ? GameResult.white_win : GameResult.black_win;
            game.WinnerColor = isWhiteTurn ? WinnerColor.white : WinnerColor.black;
            game.EndReason = EndReason.checkmate;
            game.EndedAt = now;
            MaybeUpdateRatings(game);
        }
        else if (stale)
        {
            game.Result = GameResult.draw;
            game.EndReason = EndReason.stalemate;
            game.EndedAt = now;
        }

        await db.SaveChangesAsync();
        var state = BuildState(game);
        state.LastMove = fromSq.ToLowerInvariant() + toSq.ToLowerInvariant();
        state.IsCheck = check;
        state.IsCheckmate = mate;
        state.IsStalemate = stale;
        return state;
    }

    public async Task<ChessGameState> ResignAsync(long gameId, string username)
    {
        var game = await LoadGameAsync(gameId) ?? throw new InvalidOperationException("Game không tồn tại");
        if (game.Result != null) throw new InvalidOperationException("Game đã kết thúc");

        var totalMoves = await db.Moves.CountAsync(m => m.GameId == game.Id);
        if (totalMoves < 10)
            throw new InvalidOperationException("Chỉ có thể đầu hàng sau 10 bước đi");

        var resigningWhite = string.Equals(game.WhitePlayer.Username, username, StringComparison.OrdinalIgnoreCase);
        game.Result = resigningWhite ? GameResult.black_win : GameResult.white_win;
        game.WinnerColor = resigningWhite ? WinnerColor.black : WinnerColor.white;
        game.EndReason = EndReason.resign;
        game.EndedAt = DateTime.UtcNow;
        MaybeUpdateRatings(game);
        await db.SaveChangesAsync();
        return BuildState(game);
    }

    public async Task<ChessGameState> GetGameStateAsync(long gameId)
    {
        var game = await LoadGameAsync(gameId) ?? throw new InvalidOperationException("Game không tồn tại");
        return BuildState(game);
    }

    public async Task<(long GameId, string RoomCode)?> FindActiveGameAsync(long userId)
    {
        var game = await db.Games.Include(g => g.Room)
            .Where(g => g.Result == null && (g.WhitePlayerId == userId || g.BlackPlayerId == userId))
            .OrderByDescending(g => g.StartedAt)
            .FirstOrDefaultAsync();
        return game == null ? null : (game.Id, game.Room.Code);
    }

    public async Task<List<HistoryItem>> GetHistoryAsync(long userId, int page = 0, int pageSize = 20)
    {
        var games = await db.Games.AsNoTracking()
            .Include(g => g.WhitePlayer)
            .Include(g => g.BlackPlayer)
            .Include(g => g.Room)
            .Where(g => g.Result != null && (g.WhitePlayerId == userId || g.BlackPlayerId == userId))
            .OrderByDescending(g => g.EndedAt ?? g.StartedAt)
            .Skip(page * pageSize).Take(pageSize)
            .ToListAsync();

        return games.Select(g =>
        {
            var iAmWhite = g.WhitePlayerId == userId;
            return new HistoryItem
            {
                GameId = g.Id,
                OpponentUsername = iAmWhite ? g.BlackPlayer.Username : g.WhitePlayer.Username,
                MyColor = iAmWhite ? "white" : "black",
                Result = g.Result?.ToString(),
                EloChange = iAmWhite ? g.WhiteRatingDelta : g.BlackRatingDelta,
                TimeControl = g.Room.TimeControl.ToString(),
                TimeLimitSeconds = g.Room.TimeLimitSeconds,
                EndReason = g.EndReason?.ToString(),
                IsMatchmaking = g.Room.IsMatchmaking,
                PlayedAt = g.EndedAt ?? g.StartedAt
            };
        }).ToList();
    }

    private async Task<Game?> LoadGameAsync(long gameId) =>
        await db.Games
            .Include(g => g.WhitePlayer)
            .Include(g => g.BlackPlayer)
            .Include(g => g.Room)
            .FirstOrDefaultAsync(g => g.Id == gameId);

    private ChessGameState BuildState(Game game)
    {
        var engine = SimpleChessEngine.FromFen(game.CurrentFen);
        var (w, b) = GetLiveClock(game, engine.WhiteToMove);
        var history = db.Moves.Where(m => m.GameId == game.Id).OrderBy(m => m.MoveNumber)
            .Select(m => m.San ?? $"{m.FromSquare}{m.ToSquare}").ToList();

        var check = engine.IsInCheck(engine.WhiteToMove);
        var noMoves = engine.GenerateLegalMoves().Count == 0;

        return new ChessGameState
        {
            GameId = game.Id,
            Fen = game.CurrentFen,
            MoveHistory = history,
            CurrentTurn = engine.WhiteToMove ? "white" : "black",
            IsCheck = check,
            IsCheckmate = check && noMoves,
            IsStalemate = !check && noMoves,
            Result = game.Result?.ToString(),
            EndReason = game.EndReason?.ToString(),
            WhiteUsername = game.WhitePlayer.Username,
            BlackUsername = game.BlackPlayer.Username,
            WhiteRating = game.WhitePlayer.EloRating,
            BlackRating = game.BlackPlayer.EloRating,
            WhiteTimeMs = w,
            BlackTimeMs = b,
            WhiteRatingDelta = game.WhiteRatingDelta,
            BlackRatingDelta = game.BlackRatingDelta
        };
    }

    private static (long White, long Black) GetLiveClock(Game game, bool whiteToMove)
    {
        long white = game.WhiteTimeMs, black = game.BlackTimeMs;
        if (game.Result != null) return (white, black);
        var reference = game.LastMoveAt ?? game.StartedAt;
        var elapsed = (long)Math.Max(0, (DateTime.UtcNow - reference).TotalMilliseconds);
        if (whiteToMove) white = Math.Max(0, white - elapsed);
        else black = Math.Max(0, black - elapsed);
        return (white, black);
    }

    private void MaybeUpdateRatings(Game game)
    {
        if (game.Room is not { IsMatchmaking: true }) return;
        if (game.WhiteRatingDelta != null) return;

        var white = game.WhitePlayer;
        var black = game.BlackPlayer;
        var (wd, bd) = ComputeElo(white.EloRating, black.EloRating, game.Result!.Value);
        white.EloRating = Math.Max(0, white.EloRating + wd);
        black.EloRating = Math.Max(0, black.EloRating + bd);
        game.WhiteRatingDelta = wd;
        game.BlackRatingDelta = bd;
    }

    private static (int WhiteDelta, int BlackDelta) ComputeElo(int whiteElo, int blackElo, GameResult result)
    {
        var expectedWhite = 1.0 / (1 + Math.Pow(10, (blackElo - whiteElo) / 400.0));
        var expectedBlack = 1 - expectedWhite;
        double scoreWhite = result switch
        {
            GameResult.white_win => 1,
            GameResult.black_win => 0,
            _ => 0.5
        };
        var scoreBlack = 1 - scoreWhite;
        var wd = (int)Math.Round(EloK * (scoreWhite - expectedWhite));
        var bd = (int)Math.Round(EloK * (scoreBlack - expectedBlack));
        return (wd, bd);
    }
}

public class HistoryItem
{
    public long GameId { get; set; }
    public string OpponentUsername { get; set; } = "";
    public string MyColor { get; set; } = "";
    public string? Result { get; set; }
    public int? EloChange { get; set; }
    public string TimeControl { get; set; } = "";
    public int TimeLimitSeconds { get; set; }
    public string? EndReason { get; set; }
    public bool IsMatchmaking { get; set; }
    public DateTime PlayedAt { get; set; }
}
