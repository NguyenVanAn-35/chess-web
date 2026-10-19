namespace MajestyGambit.Web.Services.Chess;

/// <summary>
/// Engine cờ tối giản: FEN, nước đi hợp lệ (gồm chiếu), phong cấp, nhập thành cơ bản.
/// Minh có thể thay bằng thư viện .NET chuyên dụng sau.
/// </summary>
public class SimpleChessEngine
{
    private readonly char[,] _board = new char[8, 8];
    public bool WhiteToMove { get; private set; } = true;
    public string Castling { get; private set; } = "KQkq";
    public string EnPassant { get; private set; } = "-";
    public int HalfMove { get; private set; }
    public int FullMove { get; private set; } = 1;

    public static SimpleChessEngine FromFen(string fen)
    {
        var engine = new SimpleChessEngine();
        engine.LoadFen(fen);
        return engine;
    }

    public void LoadFen(string fen)
    {
        var parts = fen.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var rows = parts[0].Split('/');
        for (var r = 0; r < 8; r++)
        {
            var file = 0;
            foreach (var c in rows[r])
            {
                if (char.IsDigit(c))
                {
                    var n = c - '0';
                    for (var i = 0; i < n; i++) _board[r, file++] = '.';
                }
                else _board[r, file++] = c;
            }
        }

        WhiteToMove = parts.Length < 2 || parts[1] == "w";
        Castling = parts.Length > 2 ? parts[2] : "-";
        EnPassant = parts.Length > 3 ? parts[3] : "-";
        HalfMove = parts.Length > 4 && int.TryParse(parts[4], out var hm) ? hm : 0;
        FullMove = parts.Length > 5 && int.TryParse(parts[5], out var fm) ? fm : 1;
    }

    public string ToFen()
    {
        var rows = new List<string>();
        for (var r = 0; r < 8; r++)
        {
            var empty = 0;
            var sb = new System.Text.StringBuilder();
            for (var f = 0; f < 8; f++)
            {
                if (_board[r, f] == '.') empty++;
                else
                {
                    if (empty > 0) { sb.Append(empty); empty = 0; }
                    sb.Append(_board[r, f]);
                }
            }
            if (empty > 0) sb.Append(empty);
            rows.Add(sb.ToString());
        }

        return $"{string.Join('/', rows)} {(WhiteToMove ? "w" : "b")} {Castling} {EnPassant} {HalfMove} {FullMove}";
    }

    public bool TryMove(string fromSq, string toSq, string? promotion, out string piece, out bool capture, out bool check, out bool mate, out bool stale)
    {
        piece = "?";
        capture = false;
        check = false;
        mate = false;
        stale = false;

        if (!TryParseSq(fromSq, out var fr, out var ff) || !TryParseSq(toSq, out var tr, out var tf))
            return false;

        var p = _board[fr, ff];
        if (p == '.' || char.IsUpper(p) != WhiteToMove) return false;

        var legal = GenerateLegalMoves();
        var key = $"{fromSq.ToLowerInvariant()}{toSq.ToLowerInvariant()}{(promotion ?? "").ToLowerInvariant()}";
        var match = legal.FirstOrDefault(m =>
            m.From == fromSq.ToLowerInvariant() &&
            m.To == toSq.ToLowerInvariant() &&
            (string.IsNullOrEmpty(promotion) || string.Equals(m.Promotion, promotion, StringComparison.OrdinalIgnoreCase) ||
             (string.IsNullOrEmpty(m.Promotion) && string.IsNullOrEmpty(promotion))));

        // Allow default queen promotion if omitted
        if (match.From == null)
        {
            match = legal.FirstOrDefault(m =>
                m.From == fromSq.ToLowerInvariant() &&
                m.To == toSq.ToLowerInvariant() &&
                (string.IsNullOrEmpty(promotion) ? true : string.Equals(m.Promotion, promotion, StringComparison.OrdinalIgnoreCase)));
        }

        if (match.From == null) return false;

        piece = char.ToUpperInvariant(p).ToString();
        capture = _board[tr, tf] != '.' || (char.ToLowerInvariant(p) == 'p' && ff != tf && _board[tr, tf] == '.');

        ApplyMove(match);

        var oppMoves = GenerateLegalMoves();
        check = IsInCheck(WhiteToMove);
        if (oppMoves.Count == 0)
        {
            if (check) mate = true;
            else stale = true;
        }

        return true;
    }

    public bool IsInCheck(bool whiteKing)
    {
        FindKing(whiteKing, out var kr, out var kf);
        return IsSquareAttacked(kr, kf, !whiteKing);
    }

    public List<(string From, string To, string? Promotion)> GenerateLegalMoves()
    {
        var pseudo = GeneratePseudoMoves(WhiteToMove);
        var legal = new List<(string, string, string?)>();
        foreach (var m in pseudo)
        {
            var snapshot = Snapshot();
            ApplyMove(m);
            // After ApplyMove, side to move has flipped; the mover must not be in check.
            var moverIsWhite = !WhiteToMove;
            if (!IsInCheck(moverIsWhite))
                legal.Add(m);
            Restore(snapshot);
        }
        return legal;
    }

    private List<(string From, string To, string? Promotion)> GeneratePseudoMoves(bool white)
    {
        var moves = new List<(string, string, string?)>();
        for (var r = 0; r < 8; r++)
        for (var f = 0; f < 8; f++)
        {
            var p = _board[r, f];
            if (p == '.' || char.IsUpper(p) != white) continue;
            var from = ToSq(r, f);
            switch (char.ToLowerInvariant(p))
            {
                case 'p': AddPawnMoves(moves, r, f, white, from); break;
                case 'n': AddKnightMoves(moves, r, f, white, from); break;
                case 'b': AddSlide(moves, r, f, white, from, [(-1,-1),(-1,1),(1,-1),(1,1)]); break;
                case 'r': AddSlide(moves, r, f, white, from, [(-1,0),(1,0),(0,-1),(0,1)]); break;
                case 'q': AddSlide(moves, r, f, white, from, [(-1,-1),(-1,1),(1,-1),(1,1),(-1,0),(1,0),(0,-1),(0,1)]); break;
                case 'k': AddKingMoves(moves, r, f, white, from); break;
            }
        }
        return moves;
    }

    private void AddPawnMoves(List<(string, string, string?)> moves, int r, int f, bool white, string from)
    {
        var dir = white ? -1 : 1;
        var start = white ? 6 : 1;
        var promoRow = white ? 0 : 7;
        var nr = r + dir;
        if (In(nr, f) && _board[nr, f] == '.')
        {
            AddPawnOrPromo(moves, from, nr, f, promoRow);
            if (r == start && _board[r + 2 * dir, f] == '.')
                moves.Add((from, ToSq(r + 2 * dir, f), null));
        }
        foreach (var df in new[] { -1, 1 })
        {
            var tf = f + df;
            if (!In(nr, tf)) continue;
            if (_board[nr, tf] != '.' && char.IsUpper(_board[nr, tf]) != white)
                AddPawnOrPromo(moves, from, nr, tf, promoRow);
            else if (EnPassant != "-" && ToSq(nr, tf) == EnPassant)
                moves.Add((from, EnPassant, null));
        }
    }

    private void AddPawnOrPromo(List<(string, string, string?)> moves, string from, int r, int f, int promoRow)
    {
        var to = ToSq(r, f);
        if (r == promoRow)
            foreach (var promo in new[] { "q", "r", "b", "n" })
                moves.Add((from, to, promo));
        else moves.Add((from, to, null));
    }

    private void AddKnightMoves(List<(string, string, string?)> moves, int r, int f, bool white, string from)
    {
        foreach (var (dr, df) in new[] { (-2,-1),(-2,1),(-1,-2),(-1,2),(1,-2),(1,2),(2,-1),(2,1) })
        {
            var tr = r + dr; var tf = f + df;
            if (!In(tr, tf)) continue;
            if (_board[tr, tf] == '.' || char.IsUpper(_board[tr, tf]) != white)
                moves.Add((from, ToSq(tr, tf), null));
        }
    }

    private void AddSlide(List<(string, string, string?)> moves, int r, int f, bool white, string from, (int, int)[] dirs)
    {
        foreach (var (dr, df) in dirs)
        {
            var tr = r + dr; var tf = f + df;
            while (In(tr, tf))
            {
                if (_board[tr, tf] == '.') moves.Add((from, ToSq(tr, tf), null));
                else
                {
                    if (char.IsUpper(_board[tr, tf]) != white) moves.Add((from, ToSq(tr, tf), null));
                    break;
                }
                tr += dr; tf += df;
            }
        }
    }

    private void AddKingMoves(List<(string, string, string?)> moves, int r, int f, bool white, string from)
    {
        for (var dr = -1; dr <= 1; dr++)
        for (var df = -1; df <= 1; df++)
        {
            if (dr == 0 && df == 0) continue;
            var tr = r + dr; var tf = f + df;
            if (!In(tr, tf)) continue;
            if (_board[tr, tf] == '.' || char.IsUpper(_board[tr, tf]) != white)
                moves.Add((from, ToSq(tr, tf), null));
        }

        // Castling
        if (white && r == 7 && f == 4)
        {
            if (Castling.Contains('K') && _board[7, 5] == '.' && _board[7, 6] == '.' &&
                !IsSquareAttacked(7, 4, false) && !IsSquareAttacked(7, 5, false) && !IsSquareAttacked(7, 6, false))
                moves.Add((from, "g1", null));
            if (Castling.Contains('Q') && _board[7, 3] == '.' && _board[7, 2] == '.' && _board[7, 1] == '.' &&
                !IsSquareAttacked(7, 4, false) && !IsSquareAttacked(7, 3, false) && !IsSquareAttacked(7, 2, false))
                moves.Add((from, "c1", null));
        }
        if (!white && r == 0 && f == 4)
        {
            if (Castling.Contains('k') && _board[0, 5] == '.' && _board[0, 6] == '.' &&
                !IsSquareAttacked(0, 4, true) && !IsSquareAttacked(0, 5, true) && !IsSquareAttacked(0, 6, true))
                moves.Add((from, "g8", null));
            if (Castling.Contains('q') && _board[0, 3] == '.' && _board[0, 2] == '.' && _board[0, 1] == '.' &&
                !IsSquareAttacked(0, 4, true) && !IsSquareAttacked(0, 3, true) && !IsSquareAttacked(0, 2, true))
                moves.Add((from, "c8", null));
        }
    }

    private void ApplyMove((string From, string To, string? Promotion) m)
    {
        TryParseSq(m.From, out var fr, out var ff);
        TryParseSq(m.To, out var tr, out var tf);
        var p = _board[fr, ff];
        var isPawn = char.ToLowerInvariant(p) == 'p';
        var capture = _board[tr, tf] != '.';

        // En passant capture
        if (isPawn && ff != tf && _board[tr, tf] == '.')
            _board[fr, tf] = '.';

        // Castling rook move
        if (char.ToLowerInvariant(p) == 'k' && Math.Abs(tf - ff) == 2)
        {
            if (tf == 6) { _board[tr, 5] = _board[tr, 7]; _board[tr, 7] = '.'; }
            if (tf == 2) { _board[tr, 3] = _board[tr, 0]; _board[tr, 0] = '.'; }
        }

        _board[tr, tf] = p;
        _board[fr, ff] = '.';

        if (isPawn && !string.IsNullOrEmpty(m.Promotion))
        {
            var promo = m.Promotion![0];
            _board[tr, tf] = char.IsUpper(p) ? char.ToUpperInvariant(promo) : char.ToLowerInvariant(promo);
        }

        // Update castling rights
        var castle = Castling == "-" ? "" : Castling;
        if (p == 'K') castle = castle.Replace("K", "").Replace("Q", "");
        if (p == 'k') castle = castle.Replace("k", "").Replace("q", "");
        if (m.From == "a1" || m.To == "a1") castle = castle.Replace("Q", "");
        if (m.From == "h1" || m.To == "h1") castle = castle.Replace("K", "");
        if (m.From == "a8" || m.To == "a8") castle = castle.Replace("q", "");
        if (m.From == "h8" || m.To == "h8") castle = castle.Replace("k", "");
        Castling = string.IsNullOrEmpty(castle) ? "-" : castle;

        EnPassant = "-";
        if (isPawn && Math.Abs(tr - fr) == 2)
            EnPassant = ToSq((fr + tr) / 2, ff);

        HalfMove = isPawn || capture ? 0 : HalfMove + 1;
        if (!WhiteToMove) FullMove++;
        WhiteToMove = !WhiteToMove;
    }

    private bool IsSquareAttacked(int r, int f, bool byWhite)
    {
        // Pawns
        var pr = byWhite ? r + 1 : r - 1;
        foreach (var df in new[] { -1, 1 })
            if (In(pr, f + df))
            {
                var p = _board[pr, f + df];
                if (p != '.' && char.ToLowerInvariant(p) == 'p' && char.IsUpper(p) == byWhite) return true;
            }

        foreach (var (dr, df) in new[] { (-2,-1),(-2,1),(-1,-2),(-1,2),(1,-2),(1,2),(2,-1),(2,1) })
            if (In(r + dr, f + df))
            {
                var p = _board[r + dr, f + df];
                if (p != '.' && char.ToLowerInvariant(p) == 'n' && char.IsUpper(p) == byWhite) return true;
            }

        if (IsAttackedBySlide(r, f, byWhite, [(-1,-1),(-1,1),(1,-1),(1,1)], "bq")) return true;
        if (IsAttackedBySlide(r, f, byWhite, [(-1,0),(1,0),(0,-1),(0,1)], "rq")) return true;

        for (var dr = -1; dr <= 1; dr++)
        for (var df = -1; df <= 1; df++)
        {
            if (dr == 0 && df == 0) continue;
            if (!In(r + dr, f + df)) continue;
            var p = _board[r + dr, f + df];
            if (p != '.' && char.ToLowerInvariant(p) == 'k' && char.IsUpper(p) == byWhite) return true;
        }
        return false;
    }

    private bool IsAttackedBySlide(int r, int f, bool byWhite, (int, int)[] dirs, string pieces)
    {
        foreach (var (dr, df) in dirs)
        {
            var tr = r + dr; var tf = f + df;
            while (In(tr, tf))
            {
                var p = _board[tr, tf];
                if (p != '.')
                {
                    if (char.IsUpper(p) == byWhite && pieces.Contains(char.ToLowerInvariant(p))) return true;
                    break;
                }
                tr += dr; tf += df;
            }
        }
        return false;
    }

    private void FindKing(bool white, out int r, out int f)
    {
        var k = white ? 'K' : 'k';
        for (r = 0; r < 8; r++)
        for (f = 0; f < 8; f++)
            if (_board[r, f] == k) return;
        r = f = 0;
    }

    private object Snapshot() => (
        (char[,])_board.Clone(),
        WhiteToMove, Castling, EnPassant, HalfMove, FullMove
    );

    private void Restore(object snap)
    {
        var (b, w, c, e, h, f) = ((char[,], bool, string, string, int, int))snap;
        Array.Copy(b, _board, b.Length);
        WhiteToMove = w; Castling = c; EnPassant = e; HalfMove = h; FullMove = f;
    }

    private static bool In(int r, int f) => r is >= 0 and < 8 && f is >= 0 and < 8;
    private static string ToSq(int r, int f) => $"{(char)('a' + f)}{8 - r}";
    private static bool TryParseSq(string sq, out int r, out int f)
    {
        r = f = 0;
        if (string.IsNullOrWhiteSpace(sq) || sq.Length != 2) return false;
        f = char.ToLowerInvariant(sq[0]) - 'a';
        r = 8 - (sq[1] - '0');
        return In(r, f);
    }
}
