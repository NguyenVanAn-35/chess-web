namespace MajestyGambit.Web.Models.Game;

public class ChessGameState
{
    public long GameId { get; set; }
    public string Fen { get; set; } = string.Empty;
    public string? LastMove { get; set; }
    public List<string> MoveHistory { get; set; } = [];
    public string? CurrentTurn { get; set; }
    public bool IsCheck { get; set; }
    public bool IsCheckmate { get; set; }
    public bool IsStalemate { get; set; }
    public string? Result { get; set; }
    public string? EndReason { get; set; }
    public string WhiteUsername { get; set; } = string.Empty;
    public string BlackUsername { get; set; } = string.Empty;
    public int WhiteRating { get; set; }
    public int BlackRating { get; set; }
    public long WhiteTimeMs { get; set; }
    public long BlackTimeMs { get; set; }
    public int? WhiteRatingDelta { get; set; }
    public int? BlackRatingDelta { get; set; }
}
