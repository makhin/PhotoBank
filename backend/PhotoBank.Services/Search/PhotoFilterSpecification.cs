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
            var to = filter.TakenDateTo.Value.AddDays(1).AddSeconds(-1);
            query = query.Where(p => p.TakenDate.HasValue && p.TakenDate <= to);
        }

        if (filter.ThisDay != null)
        {
            query = query.Where(p => p.TakenDate.HasValue &&
                                     (p.TakenDay != null && p.TakenDay.Value == filter.ThisDay.Day) &&
                                     (p.TakenMonth != null && p.TakenMonth.Value == filter.ThisDay.Month));
        }

        if (filter.Storages?.Any() == true)
        {
            var storages = filter.Storages.ToList();

            if (!string.IsNullOrEmpty(filter.RelativePath))
            {
                query = query.Where(p => p.Files.Any(f =>
                    storages.Contains(f.StorageId) && f.RelativePath == filter.RelativePath));
            }
            else
            {
                // Filter by Files.StorageId for cross-storage duplicate support
                query = query.Where(p => p.Files.Any(f => storages.Contains(f.StorageId)));
            }
        }

        if (!string.IsNullOrEmpty(filter.Caption))
        {
            // Full-text search � ts_vector
            query = query.Where(p => p.Captions.Any(c =>
                EF.Functions.ToTsVector("english", c.Text)
                    .Matches(EF.Functions.WebSearchToTsQuery("english", filter.Caption!))));
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

