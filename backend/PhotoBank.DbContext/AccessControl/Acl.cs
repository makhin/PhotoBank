using PhotoBank.DbContext.Models;
using System;
using System.Linq;

namespace PhotoBank.AccessControl;

public sealed record Acl(
    int[] StorageIds,
    DateTime? FromDate,
    DateTime? ToDate,
    int[] AllowedPersonGroupIds,
    bool CanSeeNsfw
)
{
    public static Acl FromUser(ICurrentUser u)
    {
        var storageIds = u.AllowedStorageIds?.Select(i => (int)i).ToArray() ?? Array.Empty<int>();
        var groups = u.AllowedPersonGroupIds?.Select(i => (int)i).ToArray() ?? Array.Empty<int>();
        DateTime? from = null, to = null;
        if (u.AllowedDateRanges?.Count > 0)
        {
            from = u.AllowedDateRanges.Min(r => r.From).ToDateTime(TimeOnly.MinValue);
            to = u.AllowedDateRanges.Max(r => r.To).ToDateTime(TimeOnly.MaxValue);
        }
        return new(storageIds, from, to, groups, u.CanSeeNsfw);
    }
}

public static class MaybeAclExtensions
{
    /// <summary>Применяет ACL для Photo только если пользователь не админ.</summary>
    public static IQueryable<Photo> MaybeApplyAcl(this IQueryable<Photo> q, ICurrentUser user)
        => user.IsAdmin ? q : q.Where(AclPredicates.PhotoWhere(Acl.FromUser(user)));

    /// <summary>Применяет ACL для Person только если пользователь не админ.</summary>
    public static IQueryable<Person> MaybeApplyAcl(this IQueryable<Person> q, ICurrentUser user)
        => user.IsAdmin ? q : q.Where(AclPredicates.PersonWhere(Acl.FromUser(user)));

    /// <summary>Применяет ACL для Storage только если пользователь не админ.</summary>
    public static IQueryable<Storage> MaybeApplyAcl(this IQueryable<Storage> q, ICurrentUser user)
        => user.IsAdmin ? q : q.Where(AclPredicates.StorageWhere(Acl.FromUser(user)));
}