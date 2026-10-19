using MajestyGambit.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MajestyGambit.Web.Controllers;

[Authorize]
public class GameController(GameService games, AuthService auth) : Controller
{
    [HttpGet("/Game/{gameId:long}")]
    public async Task<IActionResult> Play(long gameId)
    {
        try
        {
            var state = await games.GetGameStateAsync(gameId);
            var user = await auth.GetCurrentUserAsync(User);
            ViewBag.CurrentUsername = user?.Username;
            ViewBag.GameId = gameId;
            return View(state);
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Index", "Lobby");
        }
    }

    [HttpGet]
    public async Task<IActionResult> Active()
    {
        var user = await auth.GetCurrentUserAsync(User);
        if (user == null) return Unauthorized();
        var active = await games.FindActiveGameAsync(user.Id);
        if (active == null) return Ok(new { });
        return Ok(new { gameId = active.Value.GameId, roomCode = active.Value.RoomCode });
    }
}
