using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace PhotoBank.AccessControl;

public sealed class EffectiveAccessProvider : IEffectiveAccessProvider
{
    private readonly AccessControlDbContext _db;
    private readonly IMemoryCache _cache;

    public EffectiveAccessProvider(AccessControlDbContext db, IMemoryCache cache)
    {
        _db = db; _cache = cache;
    }

    public void Invalidate(string userId) => _cache.Remove(CacheKey(userId));

    public async Task<EffectiveAccess> GetAsync(string userId, ClaimsPrincipal principal, CancellationToken ct = default)
    {
        var key = CacheKey(userId);
        if (_cache.TryGetValue(key, out EffectiveAccess cached)) return cached;

        var isAdmin = principal.IsInRole("Admin");
        if (isAdmin)
        {
            var admin = new EffectiveAccess(new HashSet<int>(), new HashSet<int>(), new List<(DateOnly, DateOnly)>(), false, true);
            _cache.Set(key, admin, TimeSpan.FromMinutes(15));
            return admin;
        }

        var roleIds = principal.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .Where(v => Guid.TryParse(v, out _))
            .Select(v => Guid.Parse(v))
            .ToList();

        if (!Guid.TryParse(userId, out var userGuid))
        {
            var empty = new EffectiveAccess(new HashSet<int>(), new HashSet<int>(), new List<(DateOnly, DateOnly)>(), false, false);
            _cache.Set(key, empty, TimeSpan.FromMinutes(5));
            return empty;
        }

        var profileIds = await _db.RoleAccessProfiles
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.ProfileId)
            .Concat(_db.UserAccessProfiles.Where(up => up.UserId == userGuid).Select(up => up.ProfileId))
            .Distinct()
            .ToListAsync(ct);

        if (profileIds.Count == 0)
        {
            var empty = new EffectiveAccess(new HashSet<int>(), new HashSet<int>(), new List<(DateOnly, DateOnly)>(), false, false);
            _cache.Set(key, empty, TimeSpan.FromMinutes(5));
            return empty;
        }

        var storages = await _db.AccessProfileStorages
            .Where(x => profileIds.Contains(x.ProfileId))
            .Select(x => x.StorageId)
            .Distinct()
            .ToListAsync(ct);

        var groups = await _db.AccessProfilePersonGroups
            .Where(x => profileIds.Contains(x.ProfileId))
            .Select(x => x.PersonGroupId)
            .Distinct()
            .ToListAsync(ct);

        var ranges = await _db.AccessProfileDateRanges
            .Where(x => profileIds.Contains(x.ProfileId))
            .Select(x => new { x.FromDate, x.ToDate })
            .ToListAsync(ct);

        var canSeeNsfw = await _db.AccessProfiles
            .Where(p => profileIds.Contains(p.Id))
            .Select(p => p.Flags_CanSeeNsfw)
            .AnyAsync(x => x, ct);

        var eff = new EffectiveAccess(
            storages.ToHashSet(),
            groups.ToHashSet(),
            ranges.Select(r => (r.FromDate, r.ToDate)).ToList(),
            canSeeNsfw,
            false
        );

        _cache.Set(key, eff, TimeSpan.FromMinutes(15));
        return eff;
    }

    private static string CacheKey(string u) => $"effacc:{u}";
}
