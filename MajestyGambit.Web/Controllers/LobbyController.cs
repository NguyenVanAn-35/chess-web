using MajestyGambit.Web.Entities;
using MajestyGambit.Web.Models.Lobby;
using MajestyGambit.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MajestyGambit.Web.Controllers;

[Authorize]
public class LobbyController(AuthService auth, RoomService rooms, MatchmakingService matchmaking) : Controller
{
    public async Task<IActionResult> Index()
    {
        var user = await auth.GetCurrentUserAsync(User);
        if (user == null) return Challenge();

        return View(new LobbyViewModel
        {
            Username = user.Username,
            EloRating = user.EloRating
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRoom(CreateRoomForm form)
    {
        var user = await auth.GetCurrentUserAsync(User) ?? throw new InvalidOperationException();
        var tc = Enum.Parse<TimeControl>(form.TimeControl, ignoreCase: true);
        var room = await rooms.CreateRoomAsync(user, tc, form.TimeLimitSeconds, form.ColorPreference);
        return RedirectToAction("Index", "Room", new { code = room.Code });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> JoinRoom(string joinCode)
    {
        var user = await auth.GetCurrentUserAsync(User) ?? throw new InvalidOperationException();
        try
        {
            var room = await rooms.JoinRoomAsync(joinCode.Trim(), user);
            return RedirectToAction("Index", "Room", new { code = room.Code });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    public async Task<IActionResult> Queue([FromForm] string timeControl = "blitz", [FromForm] int timeLimitSeconds = 300)
    {
        var user = await auth.GetCurrentUserAsync(User) ?? throw new InvalidOperationException();
        await matchmaking.EnqueueAsync(user, timeControl, timeLimitSeconds);
        return Ok(new { queued = true });
    }

    [HttpPost]
    public async Task<IActionResult> LeaveQueue()
    {
        var user = await auth.GetCurrentUserAsync(User) ?? throw new InvalidOperationException();
        matchmaking.Dequeue(user);
        return Ok(new { left = true });
    }
}
