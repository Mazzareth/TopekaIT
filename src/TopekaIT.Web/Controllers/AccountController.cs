using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TopekaIT.Core.Access;
using TopekaIT.Core.Domain.Enums;
using TopekaIT.Core.Services;

namespace TopekaIT.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserService _users;
    private readonly DivisionService _divisions;

    public AccountController(UserService users, DivisionService divisions)
    {
        _users = users;
        _divisions = divisions;
    }

    [HttpGet("/")]
    public IActionResult Root()
    {
        if (!(User.Identity?.IsAuthenticated ?? false)) return Redirect("/login");
        var tier = AccessTierExtensions.ParseTierOrWorker(User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value);
        return Redirect(tier switch
        {
            AccessTier.SuperAdmin => "/admin",
            AccessTier.Admin => "/it",
            AccessTier.Supervisor => "/manager",
            _ => "/worker",
        });
    }

    [HttpPost("/auth/login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromForm] string Username, [FromForm] string Password, [FromForm] string? ReturnUrl)
    {
        var user = await _users.ValidateCredentialsAsync(Username, Password);
        if (user == null)
        {
            return Redirect($"/login?ErrorMessage={Uri.EscapeDataString("Invalid username or password")}");
        }

        await _users.MarkActiveAsync(user.Id, DateTimeOffset.UtcNow);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Username),
            new("name", user.Name),
            new("avatar", user.Avatar),
            new("division", user.DivisionId ?? ""),
            new("must_change_password", user.MustChangePassword ? "true" : "false"),
            new(ClaimTypes.Role, user.Role.ToString()),
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties { IsPersistent = true });

        if (user.MustChangePassword)
        {
            return Redirect("/change-password");
        }

        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return Redirect(ReturnUrl);
        }
        return Redirect(user.Role switch
        {
            AccessTier.SuperAdmin => "/admin",
            AccessTier.Admin => "/it",
            AccessTier.Supervisor => "/manager",
            _ => "/worker",
        });
    }

    [HttpGet("/auth/enter-division/{divisionId}")]
    [Authorize(Policy = AccessPermissionKeys.AdminEnterDivisions)]
    public async Task<IActionResult> EnterDivision(string divisionId, [FromQuery] string? ReturnUrl = null)
    {
        var division = await _divisions.GetByIdAsync(divisionId);
        if (division == null)
        {
            return Redirect("/admin");
        }

        var existingClaims = User.Claims
            .Where(c => c.Type != "active_division")
            .Select(c => new Claim(c.Type, c.Value))
            .ToList();
        existingClaims.Add(new Claim("active_division", division.Id));

        var identity = new ClaimsIdentity(existingClaims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties { IsPersistent = true });

        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return Redirect(ReturnUrl);
        }

        return Redirect("/it");
    }

    [HttpGet("/auth/logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/login");
    }
}
