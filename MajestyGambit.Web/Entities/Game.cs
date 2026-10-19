using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MajestyGambit.Web.Entities;

[Table("games")]
public class Game
{
    public const string DefaultFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    [Key]
    public long Id { get; set; }

    [Column("room_id")]
    public long RoomId { get; set; }

    [ForeignKey(nameof(RoomId))]
    public Room Room { get; set; } = null!;

    [Column("white_player_id")]
    public long WhitePlayerId { get; set; }

    [ForeignKey(nameof(WhitePlayerId))]
    public User WhitePlayer { get; set; } = null!;

    [Column("black_player_id")]
    public long BlackPlayerId { get; set; }

    [ForeignKey(nameof(BlackPlayerId))]
    public User BlackPlayer { get; set; } = null!;

    [Column("current_fen"), MaxLength(120)]
    public string CurrentFen { get; set; } = DefaultFen;

    [MaxLength(20)]
    public GameResult? Result { get; set; }

    [Column("winner_color"), MaxLength(10)]
    public WinnerColor? WinnerColor { get; set; }

    [Column("end_reason"), MaxLength(30)]
    public EndReason? EndReason { get; set; }

    [Column("white_time_ms")]
    public long WhiteTimeMs { get; set; }

    [Column("black_time_ms")]
    public long BlackTimeMs { get; set; }

    [Column("white_rating_delta")]
    public int? WhiteRatingDelta { get; set; }

    [Column("black_rating_delta")]
    public int? BlackRatingDelta { get; set; }

    [Column("last_move_at")]
    public DateTime? LastMoveAt { get; set; }

    [Column("started_at")]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    [Column("ended_at")]
    public DateTime? EndedAt { get; set; }

    public ICollection<Move> Moves { get; set; } = new List<Move>();
}
