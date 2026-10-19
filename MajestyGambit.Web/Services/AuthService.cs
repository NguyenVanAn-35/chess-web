using System.Security.Claims;
using MajestyGambit.Web.Data;
using MajestyGambit.Web.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace MajestyGambit.Web.Services;

public class AuthService(AppDbContext db)
{
    public async Task<(bool Ok, string? Error)> RegisterAsync(string username, string email, string password)
    {
        username = username.Trim();
        email = email.Trim().ToLowerInvariant();

        if (username.Length < 3) return (false, "Username tối thiểu 3 ký tự");
        if (password.Length < 6) return (false, "Password tối thiểu 6 ký tự");
        if (await db.Users.AnyAsync(u => u.Username == username))
            return (false, "Username đã tồn tại");
        if (await db.Users.AnyAsync(u => u.Email == email))
            return (false, "Email đã tồn tại");

        db.Users.Add(new User
        {
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            EloRating = 100,
            Status = UserStatus.offline
        });
        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error, User? User)> ValidateLoginAsync(string username, string password)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return (false, "Sai username hoặc password", null);

        user.Status = UserStatus.online;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return (true, null, user);
    }

    public async Task SignInAsync(HttpContext http, User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("elo", user.EloRating.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            });
    }

    public async Task SignOutAsync(HttpContext http)
    {
        if (http.User.Identity?.IsAuthenticated == true)
        {
            var idStr = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (long.TryParse(idStr, out var id))
            {
                var user = await db.Users.FindAsync(id);
                if (user != null)
                {
                    user.Status = UserStatus.offline;
                    user.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }
            }
        }
        await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    public async Task<User?> GetCurrentUserAsync(ClaimsPrincipal principal)
    {
        var idStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(idStr, out var id)) return null;
        return await db.Users.FindAsync(id);
    }
}
