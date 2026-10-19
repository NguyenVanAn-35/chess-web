using MajestyGambit.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MajestyGambit.Web.Controllers;

[Authorize]
public class RoomController(RoomService rooms, AuthService auth) : Controller
{
    [HttpGet("/Room/{code}")]
    public async Task<IActionResult> Index(string code)
    {
        try
        {
            var room = await rooms.GetRoomWithPlayersAsync(code);
            var user = await auth.GetCurrentUserAsync(User);
            ViewBag.CurrentUsername = user?.Username;
            if (room.Game != null)
                return RedirectToAction("Play", "Game", new { gameId = room.Game.Id });
            return View(room);
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Index", "Lobby");
        }
    }
}
