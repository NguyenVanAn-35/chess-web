using MajestyGambit.Web.Entities;
using Microsoft.EntityFrameworkCore;

namespace MajestyGambit.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomPlayer> RoomPlayers => Set<RoomPlayer>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Move> Moves => Set<Move>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(x => x.Username).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Status).HasConversion<string>();
        });

        modelBuilder.Entity<Room>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.TimeControl).HasConversion<string>();
            e.Property(x => x.Status).HasConversion<string>();
            e.HasOne(x => x.Host).WithMany().HasForeignKey(x => x.HostId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RoomPlayer>(e =>
        {
            e.HasIndex(x => new { x.RoomId, x.UserId }).IsUnique();
            e.Property(x => x.Color).HasConversion<string>();
            e.HasOne(x => x.Room).WithMany(r => r.Players).HasForeignKey(x => x.RoomId);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Game>(e =>
        {
            e.HasIndex(x => x.RoomId).IsUnique();
            e.Property(x => x.Result).HasConversion<string>();
            e.Property(x => x.WinnerColor).HasConversion<string>();
            e.Property(x => x.EndReason).HasConversion<string>();
            e.HasOne(x => x.Room).WithOne(r => r.Game).HasForeignKey<Game>(x => x.RoomId);
            e.HasOne(x => x.WhitePlayer).WithMany().HasForeignKey(x => x.WhitePlayerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.BlackPlayer).WithMany().HasForeignKey(x => x.BlackPlayerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Move>(e =>
        {
            e.HasIndex(x => new { x.GameId, x.MoveNumber, x.PlayerColor }).IsUnique();
            e.Property(x => x.PlayerColor).HasConversion<string>();
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasIndex(x => x.Token).IsUnique();
        });
    }
}
