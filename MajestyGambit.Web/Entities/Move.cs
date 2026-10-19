using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MajestyGambit.Web.Entities;

[Table("moves")]
public class Move
{
    [Key]
    public long Id { get; set; }

    [Column("game_id")]
    public long GameId { get; set; }

    [ForeignKey(nameof(GameId))]
    public Game Game { get; set; } = null!;

    [Column("move_number")]
    public int MoveNumber { get; set; }

    [Column("player_color"), MaxLength(10)]
    public PieceColor PlayerColor { get; set; }

    [Column("from_square"), MaxLength(2)]
    public string FromSquare { get; set; } = string.Empty;

    [Column("to_square"), MaxLength(2)]
    public string ToSquare { get; set; } = string.Empty;

    [MaxLength(2)]
    public string Piece { get; set; } = "?";

    [MaxLength(2)]
    public string? Promotion { get; set; }

    [MaxLength(20)]
    public string? San { get; set; }

    [Column("fen_after"), MaxLength(120)]
    public string FenAfter { get; set; } = string.Empty;

    [Column("time_spent_ms")]
    public int TimeSpentMs { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
