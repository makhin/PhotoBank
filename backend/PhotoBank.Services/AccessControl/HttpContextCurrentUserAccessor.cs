using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PhotoBank.AccessControl;

public sealed class HttpContextCurrentUserAccessor : ICurrentUserAccessor
{
    private static readonly object CurrentUserItemKey = new();
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEffectiveAccessProvider _effectiveAccessProvider;

    public HttpContextCurrentUserAccessor(
        IHttpContextAccessor httpContextAccessor,
        IEffectiveAccessProvider effectiveAccessProvider)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _effectiveAccessProvider = effectiveAccessProvider ?? throw new ArgumentNullException(nameof(effectiveAccessProvider));
    }

    public ICurrentUser CurrentUser
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                throw new InvalidOperationException("No HttpContext available for current user resolution.");
            }

            if (httpContext.Items.TryGetValue(CurrentUserItemKey, out var cached) && cached is ICurrentUser user)
            {
                return user;
            }

            throw new InvalidOperationException("Current user has not been resolved. Call GetCurrentUserAsync first.");
        }
    }

    public ValueTask<ICurrentUser> GetCurrentUserAsync(CancellationToken ct = default)
    {
        var httpContext = _httpContextAccessor.HttpContext
                          ?? throw new InvalidOperationException("No HttpContext available for current user resolution.");

        if (httpContext.Items.TryGetValue(CurrentUserItemKey, out var cached) && cached is ICurrentUser existing)
        {
            return ValueTask.FromResult(existing);
        }

        return ResolveAndCacheAsync(httpContext, ct);
    }

    private async ValueTask<ICurrentUser> ResolveAndCacheAsync(HttpContext httpContext, CancellationToken ct)
    {
        var principal = httpContext.User ?? throw new InvalidOperationException("No HttpContext.User available.");

        if (principal.Identity?.IsAuthenticated != true)
        {
            var anonymous = global::PhotoBank.AccessControl.CurrentUser.CreateAnonymous();
            httpContext.Items[CurrentUserItemKey] = anonymous;
            return anonymous;
        }

        var identifier = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                         ?? principal.FindFirstValue(ClaimTypes.Name)
                         ?? principal.Identity?.Name;

        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new UnauthorizedAccessException("Authenticated user missing identifier claim");
        }

        var effectiveAccess = await _effectiveAccessProvider
            .GetAsync(identifier, principal, ct)
            .ConfigureAwait(false);

        var user = global::PhotoBank.AccessControl.CurrentUser.FromEffectiveAccess(identifier, effectiveAccess);
        httpContext.Items[CurrentUserItemKey] = user;
        return user;
    }
}
