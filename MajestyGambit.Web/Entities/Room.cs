using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MajestyGambit.Web.Entities;

[Table("rooms")]
public class Room
{
    [Key]
    public long Id { get; set; }

    [Required, MaxLength(10)]
    public string Code { get; set; } = string.Empty;

    [Column("host_id")]
    public long HostId { get; set; }

    [ForeignKey(nameof(HostId))]
    public User Host { get; set; } = null!;

    [Column("time_control"), MaxLength(20)]
    public TimeControl TimeControl { get; set; } = TimeControl.blitz;

    [Column("time_limit_seconds")]
    public int TimeLimitSeconds { get; set; } = 300;

    [MaxLength(20)]
    public RoomStatus Status { get; set; } = RoomStatus.waiting;

    public ICollection<RoomPlayer> Players { get; set; } = new List<RoomPlayer>();

    public Game? Game { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("started_at")]
    public DateTime? StartedAt { get; set; }

    [Column("ended_at")]
    public DateTime? EndedAt { get; set; }

    [Column("host_color_preference"), MaxLength(10)]
    public string? HostColorPreference { get; set; }

    [Column("is_matchmaking")]
    public bool IsMatchmaking { get; set; }
}
