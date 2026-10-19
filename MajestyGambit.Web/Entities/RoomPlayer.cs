using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MajestyGambit.Web.Entities;

[Table("room_players")]
public class RoomPlayer
{
    [Key]
    public long Id { get; set; }

    [Column("room_id")]
    public long RoomId { get; set; }

    [ForeignKey(nameof(RoomId))]
    public Room Room { get; set; } = null!;

    [Column("user_id")]
    public long UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [MaxLength(10)]
    public PieceColor? Color { get; set; }

    [Column("is_ready")]
    public bool IsReady { get; set; }

    [Column("joined_at")]
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
