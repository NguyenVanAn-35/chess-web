using System.Diagnostics;
using MajestyGambit.Web.Models;
using MajestyGambit.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace MajestyGambit.Web.Controllers;

public class HomeController(AuthService auth, GameService games) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await auth.GetCurrentUserAsync(User);
            ViewBag.CurrentUser = user;
            if (user != null)
            {
                var active = await games.FindActiveGameAsync(user.Id);
                if (active != null)
                    ViewBag.ActiveGameId = active.Value.GameId;
            }
        }
        return View();
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
