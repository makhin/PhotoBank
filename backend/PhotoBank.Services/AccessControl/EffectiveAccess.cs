using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoBank.AccessControl;

public sealed record EffectiveAccess(
    IReadOnlySet<int> StorageIds,
    IReadOnlySet<int> PersonGroupIds,
    IReadOnlyList<(DateOnly From, DateOnly To)> DateRanges,
    bool NsfwOnly,
    bool IsAdmin);

public interface IEffectiveAccessProvider
{
    Task<EffectiveAccess> GetAsync(string userId, ClaimsPrincipal principal, CancellationToken ct = default);
    void Invalidate(string userId);
}
