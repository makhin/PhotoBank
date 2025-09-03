using System;
using System.Linq;
using System.Linq.Expressions;
using PhotoBank.DbContext.Models;

namespace PhotoBank.AccessControl;

public static class AclPredicates
{
    public static Expression<Func<Photo, bool>> PhotoWhere(Acl acl)
    {
        var storage = acl.StorageIds; var groups = acl.AllowedPersonGroupIds;
        var from = acl.FromDate; var to = acl.ToDate; var nsfw = acl.CanSeeNsfw;

        return p =>
            storage.Length > 0 && storage.Contains(p.StorageId) &&
            (from == null || (p.TakenDate != null && p.TakenDate >= from)) &&
            (to == null || (p.TakenDate != null && p.TakenDate <= to)) &&
            (nsfw || (!p.IsAdultContent && !p.IsRacyContent)) &&
            (
                groups.Length == 0
                    ? !p.Faces.Any() // нет групп -> только без лиц
                    : (!p.Faces.Any() || p.Faces.Any(f => f.PersonId != null &&
                                                          f.Person.PersonGroups.Any(pg => groups.Contains(pg.Id))))
            );
    }

    public static Expression<Func<Person, bool>> PersonWhere(Acl acl)
        => p => acl.AllowedPersonGroupIds.Length != 0 && p.PersonGroups.Any(pg => acl.AllowedPersonGroupIds.Contains(pg.Id));

    public static Expression<Func<Storage, bool>> StorageWhere(Acl acl)
        => s => acl.StorageIds.Length != 0 && acl.StorageIds.Contains(s.Id);
}