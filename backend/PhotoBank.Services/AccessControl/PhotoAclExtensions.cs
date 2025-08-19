using System;
using System.Linq;
using System.Collections.Generic;
using PhotoBank.DbContext.Models;

namespace PhotoBank.AccessControl;

/// <summary>
/// Расширения для безопасного применения ACL к запросам фотографий.
/// Правила:
/// 1) Storage ограничены списком разрешённых хранилищ.
/// 2) Дата (TakenDate) ограничена разрешёнными диапазонами (если заданы).
/// 3) По людям: видны фото БЕЗ лиц ИЛИ фото, где есть хотя бы одно лицо из разрешённых групп.
/// </summary>
public static class PhotoAclExtensions
{
    public sealed record PhotoAcl(
        IReadOnlyCollection<long> StorageIds,
        DateOnly? FromDate,
        DateOnly? ToDate,
        IReadOnlyCollection<long> AllowedPersonGroupIds
    );

    public static IQueryable<Photo> ApplyAcl(this IQueryable<Photo> query, PhotoAcl acl)
    {
        if (acl is null) return query; // Админ/полный доступ (обрабатывается на уровне вызывающего кода)

        // 1) storage
        if (acl.StorageIds is { Count: > 0 })
        {
            query = query.Where(p => acl.StorageIds.Contains(p.StorageId));
        }
        else
        {
            // Если нет разрешённых storage — вернуть пусто.
            return query.Where(_ => false);
        }

        // 2) date range
        if (acl.FromDate is { } from)
        {
            var fromDate = from.ToDateTime(TimeOnly.MinValue);
            query = query.Where(p => p.TakenDate >= fromDate);
        }
        if (acl.ToDate is { } to)
        {
            var toDate = to.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(p => p.TakenDate <= toDate);
        }

        // 3) persons/groups
        // Разрешаем фото без лиц ИЛИ с хотя бы одним лицом из разрешённых групп.
        // Если список групп пуст — оставляем только фото без лиц.
        if (acl.AllowedPersonGroupIds is { Count: > 0 })
        {
            query = query.Where(p =>
                !p.Faces.Any() ||
                p.Faces.Any(f => f.Person != null &&
                                 f.Person.PersonGroups.Any(pg => acl.AllowedPersonGroupIds.Contains(pg.Id)))
            );
        }
        else
        {
            query = query.Where(p => !p.Faces.Any());
        }

        return query;
    }
}
