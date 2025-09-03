using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace PhotoBank.AccessControl;

public sealed class CurrentUser : ICurrentUser
{
    public string UserId { get; }
    public bool IsAdmin { get; }
    public IReadOnlySet<int> AllowedStorageIds { get; }
    public IReadOnlySet<int> AllowedPersonGroupIds { get; }
    public IReadOnlyList<(DateOnly From, DateOnly To)> AllowedDateRanges { get; }
    public bool CanSeeNsfw { get; }

    public CurrentUser(IHttpContextAccessor http, IEffectiveAccessProvider provider)
    {
        var principal = http.HttpContext?.User ?? throw new InvalidOperationException("No HttpContext.User");

        if (principal.Identity?.IsAuthenticated != true)
        {
            UserId = principal.Identity?.Name ?? string.Empty;
            IsAdmin = false;
            AllowedStorageIds = new HashSet<int>();
            AllowedPersonGroupIds = new HashSet<int>();
            AllowedDateRanges = Array.Empty<(DateOnly From, DateOnly To)>();
            CanSeeNsfw = false;
            return;
        }

        UserId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? throw new InvalidOperationException("Authenticated user missing NameIdentifier claim");

        var eff = provider.GetAsync(UserId, principal).GetAwaiter().GetResult();

        IsAdmin = eff.IsAdmin;
        AllowedStorageIds = eff.StorageIds;
        AllowedPersonGroupIds = eff.PersonGroupIds;
        AllowedDateRanges = eff.DateRanges;
        CanSeeNsfw = eff.CanSeeNsfw;
    }
}
