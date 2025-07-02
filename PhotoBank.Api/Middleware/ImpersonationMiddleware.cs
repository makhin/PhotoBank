using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.Models;

namespace PhotoBank.Api.Middleware;

public class ImpersonationMiddleware
{
    public const string HeaderName = "X-Impersonate-User";
    private readonly RequestDelegate _next;
    private readonly UserManager<ApplicationUser> _userManager;

    public ImpersonationMiddleware(RequestDelegate next, UserManager<ApplicationUser> userManager)
    {
        _next = next;
        _userManager = userManager;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var username) && !string.IsNullOrWhiteSpace(username))
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Telegram == username);
            if (user is not null)
            {
                var claims = new List<Claim>(context.User.Claims)
                {
                    new Claim("ImpersonatedUser", username!)
                };
                var userClaims = await _userManager.GetClaimsAsync(user);
                claims.AddRange(userClaims);
                var identity = new ClaimsIdentity(claims, context.User.Identity?.AuthenticationType ?? "Impersonation");
                context.User = new ClaimsPrincipal(identity);
            }
        }

        await _next(context);
    }
}
