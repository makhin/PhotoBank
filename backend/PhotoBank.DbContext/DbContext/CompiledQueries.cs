using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PhotoBank.AccessControl;
using Photo = PhotoBank.DbContext.Models.Photo;

namespace PhotoBank.DbContext.DbContext;

public static class CompiledQueries
{
    /// <summary>
    /// Безопасный вариант: обязательны ограничения по Storage, датам и группам персон.
    /// Условие по людям: фото без лиц ИЛИ есть лицо, принадлежащее разрешённым группам.
    /// </summary>
    public static readonly Func<PhotoBankDbContext, long, int[], DateTime?, DateTime?, int[], bool, Task<Photo?>>
        PhotoByIdWithAcl = EF.CompileAsyncQuery((
                PhotoBankDbContext ctx,
                long photoId,
                int[] storageIds,
                DateTime? from,
                DateTime? to,
                int[] groups,
                bool canSeeNsfw) =>
            ctx.Photos
                .Include(p => p.PhotoTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.Faces).ThenInclude(f => f.Person).ThenInclude(per => per.PersonGroups)
                .Include(p => p.Captions)
                .AsSplitQuery()
                .Where(p => p.Id == photoId)
                .Where(AclPredicates.PhotoWhere(new Acl(storageIds, from, to, groups, canSeeNsfw)))
                .FirstOrDefault());
}
