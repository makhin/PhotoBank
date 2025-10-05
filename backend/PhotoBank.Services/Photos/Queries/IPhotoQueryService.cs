using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using ImageMagick;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using PhotoBank.AccessControl;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Internal;
using PhotoBank.Services.Models;
using PhotoBank.Services.Search;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Services.Photos.Queries;

public interface IPhotoQueryService
{
    Task<PageResponse<PhotoItemDto>> GetAllPhotosAsync(FilterDto filter, CancellationToken ct = default);
    Task<PhotoDto> GetPhotoAsync(int id);
    Task<IEnumerable<PathDto>> GetAllPathsAsync();
    Task<IEnumerable<StorageDto>> GetAllStoragesAsync();
    Task<IEnumerable<TagDto>> GetAllTagsAsync();
    Task<IEnumerable<PhotoItemDto>> FindDuplicatesAsync(int? id, string? hash, int threshold);
}

public class PhotoQueryService : IPhotoQueryService
{
    private readonly PhotoBankDbContext _db;
    private readonly IRepository<Photo> _photoRepository;
    private readonly IRepository<Storage> _storageRepository;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PhotoQueryService> _logger;
    private readonly ICurrentUser _currentUser;
    private readonly ISearchReferenceDataService _searchReferenceDataService;
    private readonly ISearchFilterNormalizer _searchFilterNormalizer;
    private readonly IMinioClient _minioClient;
    private readonly S3Options _s3;

    public PhotoQueryService(
        PhotoBankDbContext db,
        IRepository<Photo> photoRepository,
        IRepository<Storage> storageRepository,
        IMapper mapper,
        IMemoryCache cache,
        ILogger<PhotoQueryService> logger,
        ICurrentUser currentUser,
        ISearchReferenceDataService searchReferenceDataService,
        ISearchFilterNormalizer searchFilterNormalizer,
        IMinioClient minioClient,
        IOptions<S3Options> s3Options)
    {
        _db = db;
        _photoRepository = photoRepository;
        _storageRepository = storageRepository;
        _mapper = mapper;
        _cache = cache;
        _logger = logger;
        _currentUser = currentUser;
        _searchReferenceDataService = searchReferenceDataService;
        _searchFilterNormalizer = searchFilterNormalizer;
        _minioClient = minioClient;
        _s3 = s3Options?.Value ?? new S3Options();
    }

    public async Task<PageResponse<PhotoItemDto>> GetAllPhotosAsync(FilterDto filter, CancellationToken ct = default)
    {
        filter = await _searchFilterNormalizer.NormalizeAsync(filter, ct);

        var query = ApplyFilter(_photoRepository.GetAll().AsNoTracking(), filter)
            .MaybeApplyAcl(_currentUser);

        var count = await query.CountAsync(ct);

        var pageSize = Math.Min(filter.PageSize, PageRequest.MaxPageSize);
        var skip = (filter.Page - 1) * pageSize;

        var photos = await query
            .OrderByDescending(p => p.TakenDate)
            .ThenByDescending(p => p.Id)
            .Skip(skip)
            .Take(pageSize)
            .ProjectTo<PhotoItemDto>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);

        await FillUrlsAsync(photos);

        return new PageResponse<PhotoItemDto>
        {
            TotalCount = count,
            Items = photos
        };
    }

    public async Task<PhotoDto> GetPhotoAsync(int id)
    {
        PhotoDto? dto;
        if (_currentUser.IsAdmin)
        {
            var adminQuery = _db.Photos
                .Include(p => p.PhotoTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.Faces).ThenInclude(f => f.Person).ThenInclude(pg => pg.PersonGroups)
                .Include(p => p.Captions)
                .AsSplitQuery();

            var adminEntity = await adminQuery.FirstOrDefaultAsync(p => p.Id == id);
            dto = adminEntity is null ? null : _mapper.Map<Photo, PhotoDto>(adminEntity);
        }
        else
        {
            var acl = Acl.FromUser(_currentUser);
            var nonAdminQuery = _db.Photos
                .Include(p => p.PhotoTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.Faces).ThenInclude(f => f.Person).ThenInclude(pg => pg.PersonGroups)
                .Include(p => p.Captions)
                .AsSplitQuery()
                .Where(p => p.Id == id)
                .Where(AclPredicates.PhotoWhere(acl));

            var entity = await nonAdminQuery.FirstOrDefaultAsync();
            dto = entity is null ? null : _mapper.Map<Photo, PhotoDto>(entity);
        }

        if (dto != null)
        {
            await FillUrlsAsync(dto);
        }

        return dto!;
    }

    public async Task<IEnumerable<PathDto>> GetAllPathsAsync()
    {
        var cacheKey = CacheKeys.Paths(_currentUser);
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            var query = _photoRepository.GetAll()
                .AsNoTracking()
                .MaybeApplyAcl(_currentUser)
                .Where(p => p.RelativePath != null);

            var paths = await query
                .Select(p => new { p.StorageId, p.RelativePath })
                .Distinct()
                .OrderBy(p => p.StorageId)
                .ThenBy(p => p.RelativePath)
                .ToListAsync();

            return (IReadOnlyList<PathDto>)paths
                .Select(p => new PathDto
                {
                    StorageId = p.StorageId,
                    Path = p.RelativePath!
                })
                .ToList();
        }) ?? Array.Empty<PathDto>();
    }

    public async Task<IEnumerable<StorageDto>> GetAllStoragesAsync()
    {
        var cacheKey = CacheKeys.Storages(_currentUser);
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            var query = _storageRepository.GetAll()
                .AsNoTracking()
                .MaybeApplyAcl(_currentUser);

            var items = await query
                .OrderBy(p => p.Name)
                .ThenBy(p => p.Id)
                .ProjectTo<StorageDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return (IReadOnlyList<StorageDto>)items;
        }) ?? Array.Empty<StorageDto>();
    }

    public async Task<IEnumerable<TagDto>> GetAllTagsAsync()
    {
        var tags = await _searchReferenceDataService.GetTagsAsync();
        return tags;
    }

    public async Task<IEnumerable<PhotoItemDto>> FindDuplicatesAsync(int? id, string? hash, int threshold)
    {
        if (id.HasValue)
        {
            hash = await _photoRepository.GetByCondition(p => p.Id == id.Value)
                .AsNoTracking()
                .Select(p => p.ImageHash)
                .SingleOrDefaultAsync();
        }

        if (string.IsNullOrEmpty(hash))
        {
            return Array.Empty<PhotoItemDto>();
        }

        var referenceHash = new PerceptualHash(hash);
        var candidateIds = new List<int>();

        await foreach (var photo in _photoRepository.GetAll().AsNoTracking()
                           .Where(p => !id.HasValue || p.Id != id.Value)
                           .Select(p => new { p.Id, p.ImageHash })
                           .AsAsyncEnumerable())
        {
            if (ImageHashHelper.HammingDistance(referenceHash, photo.ImageHash) <= threshold)
            {
                candidateIds.Add(photo.Id);
            }
        }

        var matchedIds = candidateIds.Distinct().ToArray();
        if (matchedIds.Length == 0)
        {
            return Array.Empty<PhotoItemDto>();
        }

        var entities = await _photoRepository.GetAll().AsNoTracking()
            .Where(p => matchedIds.Contains(p.Id))
            .ToListAsync();

        var items = _mapper.Map<List<PhotoItemDto>>(entities);
        await FillUrlsAsync(items);

        return items;
    }

    private IQueryable<Photo> ApplyFilter(IQueryable<Photo> query, FilterDto filter)
    {
        if (filter.IsBW is true) query = query.Where(p => p.IsBW);
        if (filter.IsAdultContent is true) query = query.Where(p => p.IsAdultContent);
        if (filter.IsRacyContent is true) query = query.Where(p => p.IsRacyContent);
        if (filter.TakenDateFrom.HasValue)
        {
            var from = filter.TakenDateFrom.Value.Date;
            query = query.Where(p => p.TakenDate.HasValue && p.TakenDate >= from);
        }

        if (filter.TakenDateTo.HasValue)
        {
            var toExclusive = filter.TakenDateTo.Value.Date.AddDays(1);
            query = query.Where(p => p.TakenDate.HasValue && p.TakenDate < toExclusive);
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
                    _db.Faces
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
                    _db.PhotoTags
                        .Where(pt => pt.PhotoId == p.Id && tagIds.Contains(pt.TagId))
                        .Select(pt => pt.TagId)
                        .Distinct()
                        .Count() == requiredTags);
            }
        }

        return query;
    }

    private async Task FillUrlsAsync(PhotoDto dto)
    {
        dto.PreviewUrl = await GetPresignedUrlAsync(dto.S3Key_Preview, dto.Id);
    }

    private async Task FillUrlsAsync(IEnumerable<PhotoItemDto> items)
    {
        var tasks = items.Select(async dto =>
        {
            dto.ThumbnailUrl = await GetPresignedUrlAsync(dto.S3Key_Thumbnail, dto.Id);
        });
        await Task.WhenAll(tasks);
    }

    private async Task<string?> GetPresignedUrlAsync(string? key, int? photoId)
    {
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        try
        {
            return await _minioClient.PresignedGetObjectAsync(new PresignedGetObjectArgs()
                .WithBucket(_s3.Bucket)
                .WithObject(key)
                .WithExpiry(_s3.UrlExpirySeconds));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate presigned URL for photo {PhotoId} with key {S3Key}.", photoId, key);
            return null;
        }
    }
}
