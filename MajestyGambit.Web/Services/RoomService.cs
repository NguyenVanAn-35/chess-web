using MajestyGambit.Web.Data;
using MajestyGambit.Web.Entities;
using Microsoft.EntityFrameworkCore;

namespace MajestyGambit.Web.Services;

public class RoomService(AppDbContext db)
{
    private static readonly char[] CodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();

    public async Task<Room> CreateRoomAsync(User host, TimeControl timeControl, int timeLimitSeconds, string? colorPreference)
    {
        var pref = colorPreference is "white" or "black" ? colorPreference : null;
        var room = new Room
        {
            Code = await GenerateUniqueCodeAsync(),
            HostId = host.Id,
            Host = host,
            TimeControl = timeControl,
            TimeLimitSeconds = timeLimitSeconds,
            HostColorPreference = pref,
            Status = RoomStatus.waiting
        };
        db.Rooms.Add(room);
        db.RoomPlayers.Add(new RoomPlayer { Room = room, UserId = host.Id });
        await db.SaveChangesAsync();
        return room;
    }

    public async Task<Room> CreateMatchmakingRoomAsync(User p1, User p2, TimeControl timeControl, int timeLimitSeconds)
    {
        var room = new Room
        {
            Code = await GenerateUniqueCodeAsync(),
            HostId = p1.Id,
            TimeControl = timeControl,
            TimeLimitSeconds = timeLimitSeconds,
            IsMatchmaking = true,
            Status = RoomStatus.waiting
        };
        db.Rooms.Add(room);
        db.RoomPlayers.AddRange(
            new RoomPlayer { Room = room, UserId = p1.Id, IsReady = true },
            new RoomPlayer { Room = room, UserId = p2.Id, IsReady = true });
        await db.SaveChangesAsync();
        return room;
    }

    public async Task<Room> JoinRoomAsync(string code, User user)
    {
        var room = await db.Rooms.Include(r => r.Players).FirstOrDefaultAsync(r => r.Code == code.ToUpperInvariant())
            ?? throw new InvalidOperationException("Phòng không tồn tại");

        if (room.Status != RoomStatus.waiting)
            throw new InvalidOperationException("Phòng đã bắt đầu hoặc đã kết thúc");

        if (room.Players.Count >= 2)
            throw new InvalidOperationException("Phòng đã đủ 2 người");

        if (room.Players.Any(p => p.UserId == user.Id))
            throw new InvalidOperationException("Bạn đã ở trong phòng này");

        db.RoomPlayers.Add(new RoomPlayer { RoomId = room.Id, UserId = user.Id });
        await db.SaveChangesAsync();
        return room;
    }

    public async Task<bool> SetReadyAsync(string code, User user)
    {
        var player = await db.RoomPlayers
            .Include(p => p.User)
            .Include(p => p.Room)
            .FirstOrDefaultAsync(p => p.Room.Code == code.ToUpperInvariant() && p.UserId == user.Id)
            ?? throw new InvalidOperationException("Bạn chưa vào phòng này");

        player.IsReady = true;
        await db.SaveChangesAsync();

        var players = await db.RoomPlayers.Where(p => p.RoomId == player.RoomId).ToListAsync();
        return players.Count == 2 && players.All(p => p.IsReady);
    }

    public async Task<Room> GetRoomWithPlayersAsync(string code)
    {
        return await db.Rooms
            .Include(r => r.Host)
            .Include(r => r.Players).ThenInclude(p => p.User)
            .Include(r => r.Game)
            .FirstOrDefaultAsync(r => r.Code == code.ToUpperInvariant())
            ?? throw new InvalidOperationException("Phòng không tồn tại");
    }

    private async Task<string> GenerateUniqueCodeAsync()
    {
        var rand = Random.Shared;
        string code;
        do
        {
            code = new string(Enumerable.Range(0, 6).Select(_ => CodeChars[rand.Next(CodeChars.Length)]).ToArray());
        } while (await db.Rooms.AnyAsync(r => r.Code == code));
        return code;
    }
}
