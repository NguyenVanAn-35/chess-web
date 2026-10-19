using System.Security.Claims;
using MajestyGambit.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MajestyGambit.Web.Hubs;

[Authorize]
public class ChessHub(
    GameService gameService,
    RoomService roomService,
    AuthService authService) : Hub
{
    private string Username => Context.User?.Identity?.Name
        ?? throw new HubException("Chưa đăng nhập");

    private long UserId => long.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{UserId}");
        await base.OnConnectedAsync();
    }

    public async Task JoinGameGroup(long gameId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"game:{gameId}");
        await Clients.Group($"game:{gameId}").SendAsync("PlayerEvent", new { type = "player_back", username = Username });
    }

    public async Task JoinRoomGroup(string roomCode)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"room:{roomCode.ToUpperInvariant()}");
        await Clients.Group($"room:{roomCode.ToUpperInvariant()}")
            .SendAsync("RoomEvent", new { type = "player_joined", username = Username, roomCode });
    }

    public async Task SetReady(string roomCode)
    {
        var user = await authService.GetCurrentUserAsync(Context.User!)
            ?? throw new HubException("User không tồn tại");
        var bothReady = await roomService.SetReadyAsync(roomCode, user);
        await Clients.Group($"room:{roomCode.ToUpperInvariant()}")
            .SendAsync("RoomEvent", new { type = "player_ready", username = Username, roomCode });

        if (bothReady)
        {
            var room = await roomService.GetRoomWithPlayersAsync(roomCode);
            var game = await gameService.StartGameAsync(room);
            await Clients.Group($"room:{roomCode.ToUpperInvariant()}")
                .SendAsync("RoomEvent", new { type = "game_start", gameId = game.Id, roomCode = room.Code });
        }
    }

    public async Task SendMove(long gameId, string from, string to, string? promotion)
    {
        try
        {
            var state = await gameService.ProcessMoveAsync(gameId, from, to, promotion, Username);
            await Clients.Group($"game:{gameId}").SendAsync("GameState", state);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", new { error = ex.Message });
        }
    }

    public async Task Resign(long gameId)
    {
        try
        {
            var state = await gameService.ResignAsync(gameId, Username);
            await Clients.Group($"game:{gameId}").SendAsync("GameState", state);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", new { error = ex.Message });
        }
    }

    public async Task CheckTimeout(long gameId)
    {
        var state = await gameService.ProcessTimeoutAsync(gameId, force: true);
        if (state != null)
            await Clients.Group($"game:{gameId}").SendAsync("GameState", state);
    }

    public async Task SendChat(long gameId, string message)
    {
        message = (message ?? "").Trim();
        if (message.Length == 0) return;
        if (message.Length > 200) message = message[..200];
        await Clients.Group($"game:{gameId}").SendAsync("Chat", new
        {
            username = Username,
            message,
            at = DateTime.UtcNow
        });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Client nên JoinGameGroup trước; broadcast away best-effort qua caller groups không có sẵn.
        await Clients.Others.SendAsync("PlayerEvent", new { type = "player_away", username = Username });
        await base.OnDisconnectedAsync(exception);
    }
}
