using System;
using System.Linq;
using System.Collections.Generic;
using PhotoBank.DbContext.Models;

namespace PhotoBank.AccessControl;

public static class ApplyAclExtensions
{
    public static IQueryable<Photo> ApplyAcl(this IQueryable<Photo> q, Acl acl) => q.Where(AclPredicates.PhotoWhere(acl));
    public static IQueryable<Person> ApplyAcl(this IQueryable<Person> q, Acl acl) => q.Where(AclPredicates.PersonWhere(acl));
    public static IQueryable<Storage> ApplyAcl(this IQueryable<Storage> q, Acl acl) => q.Where(AclPredicates.StorageWhere(acl));
}