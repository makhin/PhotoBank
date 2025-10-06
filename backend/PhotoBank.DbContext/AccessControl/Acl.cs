using System;
using System.Linq;
using PhotoBank.DbContext.Models;

namespace PhotoBank.AccessControl;

public readonly record struct AclDateRange(DateTime From, DateTime To);

public sealed record Acl(
    int[] StorageIds,
    AclDateRange[] DateRanges,
    int[] AllowedPersonGroupIds,
    bool CanSeeNsfw
)
{
    public static Acl FromUser(ICurrentUser u)
    {
        var storageIds = u.AllowedStorageIds?.ToArray() ?? Array.Empty<int>();
        var groups = u.AllowedPersonGroupIds?.ToArray() ?? Array.Empty<int>();
        var ranges = u.AllowedDateRanges?.Count > 0
            ? u.AllowedDateRanges
                .Select(r => new AclDateRange(
                    r.From.ToDateTime(TimeOnly.MinValue),
                    r.To.ToDateTime(TimeOnly.MaxValue)))
                .ToArray()
            : Array.Empty<AclDateRange>();

        return new(storageIds, ranges, groups, u.CanSeeNsfw);
    }
}

public static class MaybeAclExtensions
{
    /// <summary>Applies ACL for photos when current user is not an admin.</summary>
    public static IQueryable<Photo> MaybeApplyAcl(this IQueryable<Photo> query, ICurrentUser user)
        => user.IsAdmin ? query : query.Where(AclPredicates.PhotoWhere(Acl.FromUser(user)));

    /// <summary>Applies ACL for persons when current user is not an admin.</summary>
    public static IQueryable<Person> MaybeApplyAcl(this IQueryable<Person> query, ICurrentUser user)
        => user.IsAdmin ? query : query.Where(AclPredicates.PersonWhere(Acl.FromUser(user)));

    /// <summary>Applies ACL for storages when current user is not an admin.</summary>
    public static IQueryable<Storage> MaybeApplyAcl(this IQueryable<Storage> query, ICurrentUser user)
        => user.IsAdmin ? query : query.Where(AclPredicates.StorageWhere(Acl.FromUser(user)));
}
