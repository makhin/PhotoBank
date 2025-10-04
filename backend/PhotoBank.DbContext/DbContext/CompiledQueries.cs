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
                .Where(p =>
                    // Keep predicate logic in sync with AclPredicates.PhotoWhere.
                    storageIds.Length > 0 && storageIds.Contains(p.StorageId) &&
                    (from == null || (p.TakenDate != null && p.TakenDate >= from)) &&
                    (to == null || (p.TakenDate != null && p.TakenDate <= to)) &&
                    (canSeeNsfw || (!p.IsAdultContent && !p.IsRacyContent)) &&
                    (
                        groups.Length == 0
                            ? !p.Faces.Any()
                            : (!p.Faces.Any() || p.Faces.Any(f => f.PersonId != null &&
                                                                f.Person.PersonGroups.Any(pg => groups.Contains(pg.Id))))
                    ))
                .FirstOrDefault());
}
