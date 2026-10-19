using Microsoft.AspNetCore.Mvc;

namespace MajestyGambit.Web.Controllers;

/// <summary>Local / offline chess — không bắt buộc đăng nhập (parity FE /chess).</summary>
public class ChessController : Controller
{
    public IActionResult Local() => View();
}
