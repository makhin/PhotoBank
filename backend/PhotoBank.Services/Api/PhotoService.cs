using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Services.Api;

public interface IPhotoService
{
    Task<QueryResult> GetAllPhotosAsync(FilterDto filter);
    Task<PhotoDto> GetPhotoAsync(int id);
    Task<IEnumerable<PersonDto>> GetAllPersonsAsync();
    Task<IEnumerable<StorageDto>> GetAllStoragesAsync();
    Task<IEnumerable<TagDto>> GetAllTagsAsync();
    Task<IEnumerable<PathDto>> GetAllPathsAsync();
    Task UpdateFaceAsync(int faceId, int personId);
}

public class PhotoService : IPhotoService
{
    private readonly IRepository<Photo> _photoRepository;
    private readonly IRepository<Face> _faceRepository;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private readonly Lazy<Task<IReadOnlyList<PersonDto>>> _persons;
    private readonly Lazy<Task<IReadOnlyList<StorageDto>>> _storages;
    private readonly Lazy<Task<IReadOnlyList<TagDto>>> _tags;
    private readonly Lazy<Task<IReadOnlyList<PathDto>>> _paths;

    private static readonly MemoryCacheEntryOptions CacheOptions = new()
        { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };

    public PhotoService(
        IRepository<Photo> photoRepository,
        IRepository<Person> personRepository,
        IRepository<Face> faceRepository,
        IRepository<Storage> storageRepository,
        IRepository<Tag> tagRepository,
        IMapper mapper,
        IMemoryCache cache)
    {
        _photoRepository = photoRepository;
        _faceRepository = faceRepository;
        _mapper = mapper;
        _cache = cache;
        _persons = new Lazy<Task<IReadOnlyList<PersonDto>>>(() =>
            GetCachedAsync("persons", () => personRepository.GetAll()
                .OrderBy(p => p.Name)
                .ProjectTo<PersonDto>(_mapper.ConfigurationProvider)
                .ToListAsync()));
        _storages = new Lazy<Task<IReadOnlyList<StorageDto>>>(() =>
            GetCachedAsync("storages", () => storageRepository.GetAll()
                .OrderBy(p => p.Name)
                .ProjectTo<StorageDto>(_mapper.ConfigurationProvider)
                .ToListAsync()));
        _tags = new Lazy<Task<IReadOnlyList<TagDto>>>(() =>
            GetCachedAsync("tags", () => tagRepository.GetAll()
                .OrderBy(p => p.Name)
                .ProjectTo<TagDto>(_mapper.ConfigurationProvider)
                .ToListAsync()));
        _paths = new Lazy<Task<IReadOnlyList<PathDto>>>(() =>
            GetCachedAsync("paths", () => photoRepository.GetAll()
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

    public async Task<QueryResult> GetAllPhotosAsync(FilterDto filter)
    {
        var query = ApplyFilter(_photoRepository.GetAll().AsNoTracking().AsSplitQuery(), filter);

        var countTask = query.CountAsync();
        var photosTask = query
            .OrderByDescending(p => p.TakenDate)
            .Skip(filter.Skip ?? 0)
            .Take(filter.Top ?? int.MaxValue)
            .ProjectTo<PhotoItemDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        await Task.WhenAll(countTask, photosTask);

        return new QueryResult
        {
            Count = countTask.Result,
            Photos = photosTask.Result
        };
    }

    public async Task<PhotoDto> GetPhotoAsync(int id)
    {
        var photo = await _photoRepository.GetAsync(id,
            q => q.Include(p => p.Faces).ThenInclude(f => f.Person)
                   .Include(p => p.Captions)
                   .Include(p => p.PhotoTags).ThenInclude(t => t.Tag));

        return _mapper.Map<Photo, PhotoDto>(photo);
    }

    public async Task<IEnumerable<PersonDto>> GetAllPersonsAsync() => await _persons.Value;

    public async Task<IEnumerable<PathDto>> GetAllPathsAsync() => await _paths.Value;

    public async Task<IEnumerable<StorageDto>> GetAllStoragesAsync() => await _storages.Value;

    public async Task<IEnumerable<TagDto>> GetAllTagsAsync() => await _tags.Value;

    public async Task UpdateFaceAsync(int faceId, int personId)
    {
        var face = new Face
        {
            Id = faceId,
            IdentifiedWithConfidence = personId == -1 ? 0 : 1,
            IdentityStatus = personId == -1 ? IdentityStatus.StopProcessing : IdentityStatus.Identified,
            PersonId = personId == -1 ? (int?)null : personId
        };
        await _faceRepository.UpdateAsync(face, f => f.PersonId, f => f.IdentifiedWithConfidence, f => f.IdentityStatus);
    }
}

