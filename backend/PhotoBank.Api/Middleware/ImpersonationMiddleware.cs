using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.Models;

namespace PhotoBank.Api.Middleware;

public class ImpersonationMiddleware
{
    public const string HeaderName = "X-Impersonate-User";
    public const string ImpersonatedPrincipalKey = "ImpersonatedPrincipal";
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
            var user = await userManager.Users.FirstOrDefaultAsync(u => u.Telegram == username.ToString());
            if (user is not null)
            {
                var userClaims = await userManager.GetClaimsAsync(user);

                var impersonatedClaims = new List<Claim>(userClaims)
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.UserName ?? string.Empty)
                };

                var impersonatedIdentity = new ClaimsIdentity(impersonatedClaims,
                    context.User.Identity?.AuthenticationType ?? "Impersonation");
                var impersonatedPrincipal = new ClaimsPrincipal(impersonatedIdentity);

                context.Items[ImpersonatedPrincipalKey] = impersonatedPrincipal;

                var claims = new List<Claim>(context.User.Claims)
                {
                    new Claim("ImpersonatedUser", username!)
                };
                claims.AddRange(impersonatedClaims);
                var identity = new ClaimsIdentity(claims,
                    context.User.Identity?.AuthenticationType ?? "Impersonation");
                context.User = new ClaimsPrincipal(identity);
            }
        }

        await _next(context);
    }
}
