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
    private readonly IRepository<Person> _personRepository;
    private readonly IRepository<Face> _faceRepository;
    private readonly IRepository<Storage> _storageRepository;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private readonly Lazy<Task<IReadOnlyList<TagDto>>> _tags;
    private readonly Lazy<Task<IReadOnlyList<PathDto>>> _paths;

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
        IMemoryCache cache)
    {
        _db = db;
        _photoRepository = photoRepository;
        _personRepository = personRepository;
        _faceRepository = faceRepository;
        _storageRepository = storageRepository;
        _mapper = mapper;
        _cache = cache;
        _tags = new Lazy<Task<IReadOnlyList<TagDto>>>(() =>
            GetCachedAsync("tags", async () => (IReadOnlyList<TagDto>)await tagRepository.GetAll()
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ProjectTo<TagDto>(_mapper.ConfigurationProvider)
                .ToListAsync()));
        _paths = new Lazy<Task<IReadOnlyList<PathDto>>>(() =>
            GetCachedAsync("paths", async () => (IReadOnlyList<PathDto>)await photoRepository.GetAll()
                .AsNoTracking()
                .ProjectTo<PathDto>(_mapper.ConfigurationProvider)
                .Distinct()
                .OrderBy(p => p.Path)
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

    private static IQueryable<Photo> ApplyFilter(IQueryable<Photo> query, FilterDto filter)
    {
        if (filter.IsBW is true) query = query.Where(p => p.IsBW);
        if (filter.IsAdultContent is true) query = query.Where(p => p.IsAdultContent);
        if (filter.IsRacyContent is true) query = query.Where(p => p.IsRacyContent);
        if (filter.TakenDateFrom.HasValue)
            query = query.Where(p => p.TakenDate.HasValue && p.TakenDate >= filter.TakenDateFrom.Value);
        if (filter.TakenDateTo.HasValue)
            query = query.Where(p => p.TakenDate.HasValue && p.TakenDate <= filter.TakenDateTo.Value);
        if (filter.ThisDay is true)
            query = query.Where(p => p.TakenDate.HasValue &&
                                     p.TakenDate.Value.Day == DateTime.Today.Day &&
                                     p.TakenDate.Value.Month == DateTime.Today.Month);
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
            var ids = filter.Persons.ToList();
            query = query.Where(p => ids.All(id => p.Faces.Any(f => f.PersonId == id)));
        }

        if (filter.Tags?.Any() == true)
        {
            var ids = filter.Tags.ToList();
            query = query.Where(p => ids.All(id => p.PhotoTags.Any(t => t.TagId == id)));
        }

        return query;
    }

    public async Task<PageResponse<PhotoItemDto>> GetAllPhotosAsync(FilterDto filter)
    {
        var query = ApplyFilter(_photoRepository.GetAll().AsNoTracking().AsSplitQuery(), filter);

        // Execute the count query first
        var count = await query.CountAsync();

        // Execute the photos query next
        var skip = (filter.Page - 1) * filter.PageSize;
        var photos = await query
            .OrderByDescending(p => p.TakenDate)
            .Skip(skip)
            .Take(filter.PageSize)
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
        var photo = await CompiledQueries.PhotoById(_db, id);
        return _mapper.Map<Photo, PhotoDto>(photo);
    }

    public async Task<IEnumerable<PersonDto>> GetAllPersonsAsync()
    {
        return await _personRepository.GetAll()
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ProjectTo<PersonDto>(_mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<IEnumerable<PathDto>> GetAllPathsAsync() => await _paths.Value;

    public async Task<IEnumerable<StorageDto>> GetAllStoragesAsync()
    {
        return await _storageRepository.GetAll()
            .AsNoTracking()
            .OrderBy(p => p.Name)
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
}

