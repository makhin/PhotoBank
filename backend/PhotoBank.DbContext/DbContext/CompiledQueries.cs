using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Photo = PhotoBank.DbContext.Models.Photo;

namespace PhotoBank.DbContext.DbContext;

public static class CompiledQueries
{
    [Obsolete("Не использовать без ACL! Используйте PhotoByIdWithAcl.")]
    public static readonly Func<PhotoBankDbContext, long, Task<Photo?>> PhotoById =
        EF.CompileAsyncQuery((PhotoBankDbContext ctx, long id) =>
            ctx.Photos
               .AsNoTracking() // минимизируем вред, но всё равно устарело
               .FirstOrDefault(p => p.Id == id));

    /// <summary>
    /// Безопасный вариант: обязательны ограничения по Storage, датам и группам персон.
    /// Условие по людям: фото без лиц ИЛИ есть лицо, принадлежащее разрешённым группам.
    /// </summary>
    public static readonly Func<
        PhotoBankDbContext,
        long,                            // photoId
        IEnumerable<long>,               // allowedStorageIds
        DateOnly?,                       // fromDate
        DateOnly?,                       // toDate
        IEnumerable<long>,               // allowedPersonGroupIds
        Task<Photo?>
    > PhotoByIdWithAcl = EF.CompileAsyncQuery((
        PhotoBankDbContext ctx,
        long photoId,
        IEnumerable<long> storageIds,
        DateOnly? fromDate,
        DateOnly? toDate,
        IEnumerable<long> personGroupIds) =>
            ctx.Photos
               .Include(p => p.PhotoTags).ThenInclude(pt => pt.Tag)
               .Include(p => p.Faces).ThenInclude(f => f.Person).ThenInclude(per => per.PersonGroups)
               .Include(p => p.Captions)
               .AsSplitQuery()
               .Where(p => p.Id == photoId)
               .Where(p => storageIds.Contains(p.StorageId))
               .Where(p => fromDate == null || (p.TakenDate.HasValue && p.TakenDate.Value >= fromDate.Value.ToDateTime(TimeOnly.MinValue)))
               .Where(p => toDate == null || (p.TakenDate.HasValue && p.TakenDate.Value <= toDate.Value.ToDateTime(TimeOnly.MaxValue)))
               .Where(p =>
                    !p.Faces.Any() ||
                    p.Faces.Any(f => f.Person != null &&
                                     f.Person.PersonGroups.Any(pg => personGroupIds.Contains(pg.Id))))
               .FirstOrDefault());
}
