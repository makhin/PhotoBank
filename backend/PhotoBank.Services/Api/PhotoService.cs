using AutoMapper;
using AutoMapper.QueryableExtensions;
using ImageMagick;
using Microsoft.AspNetCore.Http;
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
using PhotoBank.Services;
using PhotoBank.Services.Internal;
using PhotoBank.Services.Search;
using PhotoBank.ViewModel.Dto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Api;

public interface IPhotoService
{
    Task<PageResponse<PhotoItemDto>> GetAllPhotosAsync(FilterDto filter, CancellationToken ct = default);
    Task<PhotoDto> GetPhotoAsync(int id);
    Task<IEnumerable<PersonDto>> GetAllPersonsAsync();
    Task<IEnumerable<StorageDto>> GetAllStoragesAsync();
    Task<IEnumerable<TagDto>> GetAllTagsAsync();
    Task<IEnumerable<PathDto>> GetAllPathsAsync();
    Task<IEnumerable<PersonGroupDto>> GetAllPersonGroupsAsync();
    Task<PersonDto> CreatePersonAsync(string name);
    Task<PersonDto> UpdatePersonAsync(int personId, string name);
    Task DeletePersonAsync(int personId);
    Task<PersonGroupDto> CreatePersonGroupAsync(string name);
    Task<PersonGroupDto> UpdatePersonGroupAsync(int groupId, string name);
    Task DeletePersonGroupAsync(int groupId);
    Task AddPersonToGroupAsync(int groupId, int personId);
    Task RemovePersonFromGroupAsync(int groupId, int personId);
    Task<PageResponse<FaceDto>> GetFacesPageAsync(int page, int pageSize);
    Task<IEnumerable<FaceDto>> GetAllFacesAsync();
    Task UpdateFaceAsync(int faceId, int? personId);
    Task<IEnumerable<PhotoItemDto>> FindDuplicatesAsync(int? id, string? hash, int threshold);
    Task UploadPhotosAsync(IEnumerable<IFormFile> files, int storageId, string path);
 }

public class PhotoService : IPhotoService
{
    private readonly PhotoBankDbContext _db;
    private readonly IRepository<Photo> _photoRepository;
    private readonly IRepository<Face> _faceRepository;
    private readonly IRepository<Storage> _storageRepository;
    private readonly IRepository<PersonGroup> _personGroupRepository;
    private readonly IRepository<Person> _personRepository;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PhotoService> _logger;
    private readonly Lazy<Task<IReadOnlyList<PathDto>>> _paths;
    private readonly Lazy<Task<IReadOnlyList<PersonGroupDto>>> _personGroups;
    private readonly Lazy<Task<IReadOnlyList<StorageDto>>> _storages;
    private readonly ICurrentUser _currentUser;
    private readonly ISearchReferenceDataService _searchReferenceDataService;
    private readonly ISearchFilterNormalizer _searchFilterNormalizer;
    private readonly IMinioClient _minioClient;
    private readonly S3Options _s3;

    public PhotoService(
        PhotoBankDbContext db,
        IRepository<Photo> photoRepository,
        IRepository<Person> personRepository,
        IRepository<Face> faceRepository,
        IRepository<Storage> storageRepository,
        IRepository<PersonGroup> personGroupRepository,
        IMapper mapper,
        IMemoryCache cache,
        ILogger<PhotoService> logger,
        ICurrentUser currentUser,
        ISearchReferenceDataService searchReferenceDataService,
        ISearchFilterNormalizer searchFilterNormalizer,
        IMinioClient minioClient,
        IOptions<S3Options> s3Options)
    {
        _db = db;
        _photoRepository = photoRepository;
        _faceRepository = faceRepository;
        _storageRepository = storageRepository;
        _personGroupRepository = personGroupRepository;
        _personRepository = personRepository;
        _mapper = mapper;
        _cache = cache;
        _logger = logger;
        _currentUser = currentUser;
        _searchReferenceDataService = searchReferenceDataService;
        _searchFilterNormalizer = searchFilterNormalizer;
        _minioClient = minioClient;
        _s3 = s3Options?.Value ?? new S3Options();

        _paths = new Lazy<Task<IReadOnlyList<PathDto>>>(() =>
            _cache.GetOrCreateAsync(CacheKeys.Paths(_currentUser), async () =>
            {
                var query = photoRepository.GetAll()
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
            }));

        _personGroups = new Lazy<Task<IReadOnlyList<PersonGroupDto>>>(() =>
            _cache.GetOrCreateAsync(CacheKeys.PersonGroups, async () =>
            {
                var groups = await personGroupRepository.GetAll()
                    .AsNoTracking()
                    .OrderBy(pg => pg.Name).ThenBy(pg => pg.Id)
                    .ProjectTo<PersonGroupDto>(_mapper.ConfigurationProvider)
                    .ToListAsync();

                foreach (var group in groups)
                {
                    group.Persons ??= [];
                }

                return (IReadOnlyList<PersonGroupDto>)groups;
            }));

        _storages = new Lazy<Task<IReadOnlyList<StorageDto>>>(() =>
            _cache.GetOrCreateAsync(CacheKeys.Storages(_currentUser), async () =>
            {
                var q = _storageRepository.GetAll()
                    .AsNoTracking()
                    .MaybeApplyAcl(_currentUser);

                return (IReadOnlyList<StorageDto>)await q
                    .OrderBy(p => p.Name).ThenBy(p => p.Id)
                    .ProjectTo<StorageDto>(_mapper.ConfigurationProvider)
                    .ToListAsync();
            }));
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
            query = query.Where(p => p.TakenDate.HasValue &&
                                     p.TakenDate.Value.Day == filter.ThisDay.Day &&
                                     p.TakenDate.Value.Month == filter.ThisDay.Month);
        if (filter.Storages?.Any() == true)
        {
            var storages = filter.Storages.ToList();
            query = query.Where(p => storages.Contains(p.StorageId));

            if (!string.IsNullOrEmpty(filter.RelativePath))
                query = query.Where(p => p.RelativePath == filter.RelativePath);
        }

        if (!string.IsNullOrEmpty(filter.Caption))
            query = query.Where(p => p.Captions.Any(c => EF.Functions.FreeText(c.Text, filter.Caption!)));

        if (filter.Persons?.Any() == true)
        {
            var personIds = filter.Persons.Distinct().ToArray();
            var requiredPersons = personIds.Length;

            if (requiredPersons > 0)
            {
                query = query.Where(p =>
                    _db.Faces
                        .Where(f => f.PhotoId == p.Id && f.PersonId != null && personIds.Contains(f.PersonId.Value))
                        .Select(f => f.PersonId.Value)
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

    public async Task<PageResponse<PhotoItemDto>> GetAllPhotosAsync(FilterDto filter, CancellationToken ct = default)
    {
        filter = await _searchFilterNormalizer.NormalizeAsync(filter, ct);

        var query = ApplyFilter(_photoRepository.GetAll().AsNoTracking(), filter)
            .MaybeApplyAcl(_currentUser);

        int? count = null;
        count = await query.CountAsync(ct); // ïðè æåëàíèè ìîæíî ñ÷èòàòü òîëüêî íà 1-é ñòðàíèöå

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
            TotalCount = count ?? 0,
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
            await FillUrlsAsync(dto);

        return dto!;
    }

    public async Task<IEnumerable<FaceDto>> GetAllFacesAsync()
    {
        var faces = await _faceRepository.GetAll()
            .OrderBy(f => f.Id)
            .ProjectTo<FaceDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        await FillUrlsAsync(faces);

        return faces;
    }

    public async Task<PageResponse<FaceDto>> GetFacesPageAsync(int page, int pageSize)
    {
        var boundedPage = Math.Max(1, page);
        var boundedPageSize = Math.Max(1, pageSize);

        var query = _faceRepository.GetAll()
            .AsNoTracking();

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(f => f.Id)
            .Skip((boundedPage - 1) * boundedPageSize)
            .Take(boundedPageSize)
            .ProjectTo<FaceDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        await FillUrlsAsync(items);

        return new PageResponse<FaceDto>
        {
            TotalCount = totalCount,
            Items = items,
        };
    }

    public async Task UpdateFaceAsync(int faceId, int? personId)
    {
        var face = new Face
        {
            Id = faceId,
            IdentifiedWithConfidence = personId == -1 ? 0 : 1,
            IdentityStatus = personId == -1 ? IdentityStatus.StopProcessing : IdentityStatus.Identified,
            PersonId = personId == -1 ? null : personId
        };
        await _faceRepository.UpdateAsync(face, f => f.PersonId, f => f.IdentifiedWithConfidence, f => f.IdentityStatus);
    }

    public async Task<IEnumerable<PersonDto>> GetAllPersonsAsync() => await _searchReferenceDataService.GetPersonsAsync();
    public async Task<IEnumerable<PathDto>> GetAllPathsAsync() => await _paths.Value;
    public async Task<IEnumerable<StorageDto>> GetAllStoragesAsync() => await _storages.Value;
    public async Task<IEnumerable<TagDto>> GetAllTagsAsync() => await _searchReferenceDataService.GetTagsAsync();
    public async Task<IEnumerable<PersonGroupDto>> GetAllPersonGroupsAsync() => await _personGroups.Value;

    public async Task<PersonDto> CreatePersonAsync(string name)
    {
        var entity = await _personRepository.InsertAsync(new Person { Name = name });
        InvalidatePersonsCache();
        return _mapper.Map<PersonDto>(entity);
    }

    public async Task<PersonDto> UpdatePersonAsync(int personId, string name)
    {
        var entity = new Person { Id = personId, Name = name };
        await _personRepository.UpdateAsync(entity, p => p.Name);
        InvalidatePersonsCache();
        return _mapper.Map<PersonDto>(entity);
    }

    public async Task DeletePersonAsync(int personId)
    {
        await _personRepository.DeleteAsync(personId);
        InvalidatePersonsCache();
    }

    public async Task<PersonGroupDto> CreatePersonGroupAsync(string name)
    {
        var entity = await _personGroupRepository.InsertAsync(new PersonGroup { Name = name });
        _cache.Remove(CacheKeys.PersonGroups);
        return _mapper.Map<PersonGroupDto>(entity);
    }

    public async Task<PersonGroupDto> UpdatePersonGroupAsync(int groupId, string name)
    {
        var entity = new PersonGroup { Id = groupId, Name = name };
        await _personGroupRepository.UpdateAsync(entity, pg => pg.Name);
        _cache.Remove(CacheKeys.PersonGroups);
        return _mapper.Map<PersonGroupDto>(entity);
    }

    public async Task DeletePersonGroupAsync(int groupId)
    {
        await _personGroupRepository.DeleteAsync(groupId);
        _cache.Remove(CacheKeys.PersonGroups);
    }

    public async Task AddPersonToGroupAsync(int groupId, int personId)
    {
        var person = await _db.Persons.Include(p => p.PersonGroups)
            .SingleOrDefaultAsync(p => p.Id == personId) ?? throw new ArgumentException($"Person {personId} not found", nameof(personId));
        var group = await _db.PersonGroups.FindAsync(groupId) ?? throw new ArgumentException($"Group {groupId} not found", nameof(groupId));

        if (person.PersonGroups.All(pg => pg.Id != groupId))
        {
            person.PersonGroups.Add(group);
            await _db.SaveChangesAsync();
            _cache.Remove(CacheKeys.PersonGroups);
        }
    }

    public async Task RemovePersonFromGroupAsync(int groupId, int personId)
    {
        var person = await _db.Persons.Include(p => p.PersonGroups)
            .SingleOrDefaultAsync(p => p.Id == personId) ?? throw new ArgumentException($"Person {personId} not found", nameof(personId));
        var group = person.PersonGroups.FirstOrDefault(pg => pg.Id == groupId);
        if (group != null)
        {
            person.PersonGroups.Remove(group);
            await _db.SaveChangesAsync();
            _cache.Remove(CacheKeys.PersonGroups);
        }
    }

    public async Task UploadPhotosAsync(IEnumerable<IFormFile> files, int storageId, string path)
    {
        if (files == null || !files.Any())
            return;

        var storage = await _storageRepository.GetAsync(storageId);
        if (storage == null)
            throw new ArgumentException($"Storage {storageId} not found", nameof(storageId));

        var targetPath = Path.Combine(storage.Folder, path ?? string.Empty);

        if (!Directory.Exists(targetPath))
            Directory.CreateDirectory(targetPath);

        foreach (var file in files)
        {
            var destination = Path.Combine(targetPath, file.FileName);

            if (System.IO.File.Exists(destination))
            {
                var existing = new FileInfo(destination);
                if (existing.Length == file.Length)
                {
                    continue; // same name and size, skip
                }

                var name = Path.GetFileNameWithoutExtension(file.FileName);
                var extension = Path.GetExtension(file.FileName);
                var index = 1;
                do
                {
                    var newFileName = $"{name}_{index}{extension}";
                    destination = Path.Combine(targetPath, newFileName);
                    index++;
                } while (System.IO.File.Exists(destination));
            }

            await using var stream = new FileStream(destination, FileMode.Create);
            await file.CopyToAsync(stream);
        }
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
            return [];
        }

        var referenceHash = new PerceptualHash(hash);

        var candidateIds = new List<int>();

        await foreach (var photo in _photoRepository.GetAll().AsNoTracking()
                           .Where(p => (!id.HasValue || p.Id != id.Value))
                           .Select(p => new { p.Id, p.ImageHash, p.S3Key_Thumbnail })
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
            return [];
        }

        var result = await _photoRepository.GetAll().AsNoTracking()
            .Where(p => matchedIds.Contains(p.Id))
            .ToListAsync();

        var items = _mapper.Map<List<PhotoItemDto>>(result);
        await FillUrlsAsync(items);

        return items;
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

    private async Task FillUrlsAsync(IEnumerable<FaceDto> faces)
    {
        var tasks = faces.Select(async dto =>
        {
            dto.ImageUrl = await GetPresignedUrlAsync(dto.S3Key_Image, dto.PhotoId);
        });
        await Task.WhenAll(tasks);
    }

    private async Task<string?> GetPresignedUrlAsync(string? key, int? photoId)
    {
        if (string.IsNullOrEmpty(key))
            return null;

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

    private void InvalidatePersonsCache()
    {
        _searchReferenceDataService.InvalidatePersonsCache();
    }
}
