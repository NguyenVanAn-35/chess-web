using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MajestyGambit.Web.Entities;

[Table("users")]
public class User
{
    [Key]
    public long Id { get; set; }

    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("full_name"), MaxLength(150)]
    public string? FullName { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [Column("avatar_url"), MaxLength(500)]
    public string? AvatarUrl { get; set; }

    [Column("elo_rating")]
    public int EloRating { get; set; } = 100;

    [MaxLength(20)]
    public UserStatus Status { get; set; } = UserStatus.offline;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
