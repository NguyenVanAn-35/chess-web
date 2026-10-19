using MajestyGambit.Web.Models.Auth;
using MajestyGambit.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MajestyGambit.Web.Controllers;

public class AccountController(AuthService auth) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        var (ok, error, user) = await auth.ValidateLoginAsync(model.Username, model.Password);
        if (!ok || user == null)
        {
            ModelState.AddModelError(string.Empty, error ?? "Đăng nhập thất bại");
            return View(model);
        }

        await auth.SignInAsync(HttpContext, user);
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Index", "Lobby");
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var (ok, error) = await auth.RegisterAsync(model.Username, model.Email, model.Password);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "Đăng ký thất bại");
            return View(model);
        }

        TempData["Message"] = "Đăng ký thành công. Hãy đăng nhập.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await auth.SignOutAsync(HttpContext);
        return RedirectToAction("Index", "Home");
    }
}
