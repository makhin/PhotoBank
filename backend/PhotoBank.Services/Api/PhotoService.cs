using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.ViewModel.Dto;
using PhotoBank.Services;
using System.IO;
using PhotoBank.AccessControl;

namespace PhotoBank.Services.Api;

public interface IPhotoService
{
    Task<PageResponse<PhotoItemDto>> GetAllPhotosAsync(FilterDto filter);
    Task<PhotoDto> GetPhotoAsync(int id);
    Task<IEnumerable<PersonDto>> GetAllPersonsAsync();
    Task<IEnumerable<StorageDto>> GetAllStoragesAsync();
    Task<IEnumerable<TagDto>> GetAllTagsAsync();
    Task<IEnumerable<PathDto>> GetAllPathsAsync();
    Task UpdateFaceAsync(int faceId, int personId);
    Task<IEnumerable<PhotoItemDto>> FindDuplicatesAsync(int? id, string? hash, int threshold);
    Task UploadPhotosAsync(IEnumerable<IFormFile> files, int storageId, string path);
}

public class PhotoService : IPhotoService
{
    private readonly PhotoBankDbContext _db;
    private readonly IRepository<Photo> _photoRepository;
    private readonly IRepository<Face> _faceRepository;
    private readonly IRepository<Storage> _storageRepository;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private readonly Lazy<Task<IReadOnlyList<TagDto>>> _tags;
    private readonly Lazy<Task<IReadOnlyList<PathDto>>> _paths;
    private readonly Lazy<Task<IReadOnlyList<PersonDto>>> _persons;
    private readonly ICurrentUser _currentUser;

    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpiration = null, // Setting AbsoluteExpiration to null
        SlidingExpiration = null  // Optional: SlidingExpiration can also be null for endless expiration
    };

    public PhotoService(
        PhotoBankDbContext db,
        IRepository<Photo> photoRepository,
        IRepository<Person> personRepository,
        IRepository<Face> faceRepository,
        IRepository<Storage> storageRepository,
        IRepository<Tag> tagRepository,
        IMapper mapper,
        IMemoryCache cache,
        ICurrentUser currentUser)
    {
        _db = db;
        _photoRepository = photoRepository;
        _faceRepository = faceRepository;
        _storageRepository = storageRepository;
        _mapper = mapper;
        _cache = cache;
        _currentUser = currentUser;
        _tags = new Lazy<Task<IReadOnlyList<TagDto>>>(() =>
            GetCachedAsync("tags", async () => (IReadOnlyList<TagDto>)await tagRepository.GetAll()
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ThenBy(p => p.Id)
                .ProjectTo<TagDto>(_mapper.ConfigurationProvider)
                .ToListAsync()));
        _paths = new Lazy<Task<IReadOnlyList<PathDto>>>(() =>
            GetCachedAsync("paths", async () => (IReadOnlyList<PathDto>)await photoRepository.GetAll()
                .AsNoTracking()
                .ProjectTo<PathDto>(_mapper.ConfigurationProvider)
                .Distinct()
                .OrderBy(p => p.Path)
                .ThenBy(p => p.StorageId)
                .ToListAsync()));
        _persons = new Lazy<Task<IReadOnlyList<PersonDto>>>(() =>
            GetCachedAsync("persons", async () => (IReadOnlyList<PersonDto>)await personRepository.GetAll()
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ThenBy(p => p.Id)
                .ProjectTo<PersonDto>(_mapper.ConfigurationProvider)
                .ToListAsync()));
    }

    private async Task<IReadOnlyList<T>> GetCachedAsync<T>(string key, Func<Task<IReadOnlyList<T>>> factory)
    {
        if (!_cache.TryGetValue(key, out IReadOnlyList<T> values))
        {
            values = await factory();
            _cache.Set(key, values, CacheOptions);
        }

        return values;
    }

    private IQueryable<Photo> ApplyFilter(IQueryable<Photo> query, FilterDto filter)
    {
        if (filter.IsBW is true) query = query.Where(p => p.IsBW);
        if (filter.IsAdultContent is true) query = query.Where(p => p.IsAdultContent);
        if (filter.IsRacyContent is true) query = query.Where(p => p.IsRacyContent);
        if (filter.TakenDateFrom.HasValue)
            query = query.Where(p => p.TakenDate.HasValue && p.TakenDate >= filter.TakenDateFrom.Value);
        if (filter.TakenDateTo.HasValue)
            query = query.Where(p => p.TakenDate.HasValue && p.TakenDate <= filter.TakenDateTo.Value);
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
            var personIds = filter.Persons.ToList();
            query =
                from p in query
                join f in _db.Faces on p.Id equals f.PhotoId
                where f.PersonId != null && personIds.Contains(f.PersonId.Value)
                group f by p into g
                where g.Select(x => x.PersonId).Distinct().Count() == personIds.Count
                select g.Key;
        }

        if (filter.Tags?.Any() == true)
        {
            var tagIds = filter.Tags.ToList();
            query =
                from p in query
                join pt in _db.PhotoTags on p.Id equals pt.PhotoId
                where tagIds.Contains(pt.TagId)
                group pt by p into g
                where g.Select(x => x.TagId).Distinct().Count() == tagIds.Count
                select g.Key;
        }

        return query;
    }

    public async Task<PageResponse<PhotoItemDto>> GetAllPhotosAsync(FilterDto filter)
    {
        var query = ApplyFilter(_photoRepository.GetAll().AsNoTracking().AsSplitQuery(), filter);

        // Apply ACL for non-admin users
        if (!_currentUser.IsAdmin)
        {
            var acl = BuildPhotoAcl(_currentUser);
            query = query.ApplyAcl(acl);
        }

        // Execute the count query first
        var count = await query.CountAsync();

        // Cap page size to avoid large requests
        var pageSize = Math.Min(filter.PageSize, PageRequest.MaxPageSize);

        // Execute the photos query next
        var skip = (filter.Page - 1) * pageSize;
        var photos = await query
            .OrderByDescending(p => p.TakenDate)
            .ThenByDescending(p => p.Id)
            .Skip(skip)
            .Take(pageSize)
            .ProjectTo<PhotoItemDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        return new PageResponse<PhotoItemDto>
        {
            TotalCount = count,
            Items = photos
        };
    }

    public async Task<PhotoDto> GetPhotoAsync(int id)
    {
        if (_currentUser.IsAdmin)
        {
            var adminQuery = _db.Photos
                .Include(p => p.PhotoTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.Faces).ThenInclude(f => f.Person).ThenInclude(per => per.PersonGroups)
                .Include(p => p.Captions)
                .AsSplitQuery();

            var adminEntity = await adminQuery.FirstOrDefaultAsync(p => p.Id == id);
            return adminEntity is null ? null : _mapper.Map<Photo, PhotoDto>(adminEntity);
        }

        var acl = BuildPhotoAcl(_currentUser);
        var entity = await CompiledQueries.PhotoByIdWithAcl(
            _db,
            id,
            acl.StorageIds,
            acl.FromDate,
            acl.ToDate,
            acl.AllowedPersonGroupIds
        );
        return entity is null ? null : _mapper.Map<Photo, PhotoDto>(entity);
    }

    public async Task<IEnumerable<PersonDto>> GetAllPersonsAsync() => await _persons.Value;

    public async Task<IEnumerable<PathDto>> GetAllPathsAsync() => await _paths.Value;

    public async Task<IEnumerable<StorageDto>> GetAllStoragesAsync()
    {
        return await _storageRepository.GetAll()
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .ProjectTo<StorageDto>(_mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<IEnumerable<TagDto>> GetAllTagsAsync() => await _tags.Value;

    public async Task UpdateFaceAsync(int faceId, int personId)
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
                string newFileName;
                do
                {
                    newFileName = $"{name}_{index}{extension}";
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
            return Enumerable.Empty<PhotoItemDto>();
        }

        var photos = await _photoRepository.GetAll().AsNoTracking()
            .Where(p => p.ImageHash != null && (!id.HasValue || p.Id != id.Value))
            .ToListAsync();

        var result = photos
            .Where(p => ImageHashHelper.HammingDistance(hash, p.ImageHash) <= threshold)
            .ToList();

        return _mapper.Map<IEnumerable<PhotoItemDto>>(result);
    }

    private static PhotoAclExtensions.PhotoAcl BuildPhotoAcl(ICurrentUser user)
    {
        var storageIds = user.AllowedStorageIds.Select(id => (long)id).ToArray();
        var personGroupIds = user.AllowedPersonGroupIds.Select(id => (long)id).ToArray();
        DateOnly? from = null;
        DateOnly? to = null;
        if (user.AllowedDateRanges.Count > 0)
        {
            from = user.AllowedDateRanges.Min(r => r.From);
            to = user.AllowedDateRanges.Max(r => r.To);
        }
        return new PhotoAclExtensions.PhotoAcl(storageIds, from, to, personGroupIds);
    }
}

