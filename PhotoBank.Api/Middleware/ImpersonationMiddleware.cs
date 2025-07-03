using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.Models;

namespace PhotoBank.Api.Middleware;

public class ImpersonationMiddleware
{
    public const string HeaderName = "X-Impersonate-User";
    private readonly RequestDelegate _next;

    public ImpersonationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Resolve UserManager<ApplicationUser> from the request's scoped services
        var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();

        if (context.Request.Headers.TryGetValue(HeaderName, out var username) && !string.IsNullOrWhiteSpace(username))
        {
            var user = await userManager.Users.FirstOrDefaultAsync(u => u.Telegram == username);
            if (user is not null)
            {
                var claims = new List<Claim>(context.User.Claims)
                {
                    new Claim("ImpersonatedUser", username!)
                };
                var userClaims = await userManager.GetClaimsAsync(user);
                claims.AddRange(userClaims);
                var identity = new ClaimsIdentity(claims, context.User.Identity?.AuthenticationType ?? "Impersonation");
                context.User = new ClaimsPrincipal(identity);
            }
        }

        await _next(context);
    }
}
