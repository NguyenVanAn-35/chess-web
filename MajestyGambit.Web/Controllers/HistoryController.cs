using MajestyGambit.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MajestyGambit.Web.Controllers;

[Authorize]
public class HistoryController(GameService games, AuthService auth) : Controller
{
    public async Task<IActionResult> Index(int page = 0)
    {
        var user = await auth.GetCurrentUserAsync(User);
        if (user == null) return Challenge();
        var items = await games.GetHistoryAsync(user.Id, page);
        ViewBag.Page = page;
        ViewBag.Username = user.Username;
        ViewBag.Elo = user.EloRating;
        return View(items);
    }
}
