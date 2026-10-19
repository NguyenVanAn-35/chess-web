using System.Collections.Concurrent;
using MajestyGambit.Web.Data;
using MajestyGambit.Web.Entities;
using MajestyGambit.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MajestyGambit.Web.Services;

public class MatchmakingService(
    IServiceScopeFactory scopeFactory,
    IHubContext<ChessHub> hub)
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<QueueEntry>> _queues = new();

    public async Task EnqueueAsync(User user, string timeControl, int timeLimitSeconds)
    {
        var timeKey = $"{timeControl.ToLowerInvariant()}:{timeLimitSeconds}";
        var myElo = user.EloRating;
        RemoveFromAllQueues(user.Id);

        var queue = _queues.GetOrAdd(timeKey, _ => new ConcurrentQueue<QueueEntry>());

        QueueEntry? best = null;
        var bestDiff = int.MaxValue;
        var buffer = new List<QueueEntry>();
        while (queue.TryDequeue(out var entry))
        {
            if (entry.UserId == user.Id) continue;
            var diff = Math.Abs(entry.EloRating - myElo);
            if (diff < bestDiff)
            {
                if (best != null) buffer.Add(best);
                best = entry;
                bestDiff = diff;
            }
            else buffer.Add(entry);
        }
        foreach (var e in buffer) queue.Enqueue(e);

        if (best != null)
        {
            await StartMatchedGameAsync(user.Id, best.UserId, timeControl, timeLimitSeconds);
        }
        else
        {
            queue.Enqueue(new QueueEntry(user.Id, user.Username, myElo, timeControl, timeLimitSeconds));
        }
    }

    public void Dequeue(User user) => RemoveFromAllQueues(user.Id);

    private void RemoveFromAllQueues(long userId)
    {
        foreach (var q in _queues.Values)
        {
            var kept = new List<QueueEntry>();
            while (q.TryDequeue(out var e))
                if (e.UserId != userId) kept.Add(e);
            foreach (var e in kept) q.Enqueue(e);
        }
    }

    private async Task StartMatchedGameAsync(long userId1, long userId2, string timeControl, int timeLimitSeconds)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rooms = scope.ServiceProvider.GetRequiredService<RoomService>();
        var games = scope.ServiceProvider.GetRequiredService<GameService>();

        var p1 = await db.Users.FindAsync(userId1) ?? throw new InvalidOperationException("User1 not found");
        var p2 = await db.Users.FindAsync(userId2) ?? throw new InvalidOperationException("User2 not found");
        var tc = Enum.Parse<TimeControl>(timeControl, ignoreCase: true);
        var room = await rooms.CreateMatchmakingRoomAsync(p1, p2, tc, timeLimitSeconds);
        var game = await games.StartGameAsync(room);

        var payload = new { type = "match_found", gameId = game.Id, roomCode = room.Code };
        await hub.Clients.User(p1.Id.ToString()).SendAsync("MatchFound", payload);
        await hub.Clients.User(p2.Id.ToString()).SendAsync("MatchFound", payload);
    }

    private record QueueEntry(long UserId, string Username, int EloRating, string TimeControl, int TimeLimitSeconds);
}
