using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PhotoBank.AccessControl;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Services.Search;

public class PhotoFilterSpecification
{
    private readonly PhotoBankDbContext _dbContext;

    public PhotoFilterSpecification(PhotoBankDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IQueryable<Photo> Build(FilterDto filter, ICurrentUser currentUser)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(currentUser);

        var query = _dbContext.Photos.AsNoTracking();

        if (filter.IsBW is true)
        {
            query = query.Where(p => p.IsBW);
        }

        if (filter.IsAdultContent is true)
        {
            query = query.Where(p => p.IsAdultContent);
        }

        if (filter.IsRacyContent is true)
        {
            query = query.Where(p => p.IsRacyContent);
        }

        if (filter.TakenDateFrom.HasValue)
        {
            var from = filter.TakenDateFrom.Value.Date;
            query = query.Where(p => p.TakenDate.HasValue && p.TakenDate >= from);
        }

        if (filter.TakenDateTo.HasValue)
        {
            var to = filter.TakenDateTo.Value;
            query = query.Where(p => p.TakenDate.HasValue && p.TakenDate <= to);
        }

        if (filter.ThisDay != null)
        {
            query = query.Where(p => p.TakenDate.HasValue &&
                                     p.TakenDate.Value.Day == filter.ThisDay.Day &&
                                     p.TakenDate.Value.Month == filter.ThisDay.Month);
        }

        if (filter.Storages?.Any() == true)
        {
            var storages = filter.Storages.ToList();
            query = query.Where(p => storages.Contains(p.StorageId));

            if (!string.IsNullOrEmpty(filter.RelativePath))
            {
                query = query.Where(p => p.RelativePath == filter.RelativePath);
            }
        }

        if (!string.IsNullOrEmpty(filter.Caption))
        {
            query = query.Where(p => p.Captions.Any(c => EF.Functions.FreeText(c.Text, filter.Caption!)));
        }

        if (filter.Persons?.Any() == true)
        {
            var personIds = filter.Persons.Distinct().ToArray();
            var requiredPersons = personIds.Length;

            if (requiredPersons > 0)
            {
                query = query.Where(p =>
                    _dbContext.Faces
                        .Where(f => f.PhotoId == p.Id && f.PersonId != null && personIds.Contains(f.PersonId.Value))
                        .Select(f => f.PersonId!.Value)
                        .Distinct()
                        .Count() == requiredPersons);
            }
        }

        if (filter.Tags?.Any() == true)
        {
            var tagIds = filter.Tags.Distinct().ToArray();
            var requiredTags = tagIds.Length;

            if (requiredTags > 0)
            {
                query = query.Where(p =>
                    _dbContext.PhotoTags
                        .Where(pt => pt.PhotoId == p.Id && tagIds.Contains(pt.TagId))
                        .Select(pt => pt.TagId)
                        .Distinct()
                        .Count() == requiredTags);
            }
        }

        return query.MaybeApplyAcl(currentUser);
    }
}

