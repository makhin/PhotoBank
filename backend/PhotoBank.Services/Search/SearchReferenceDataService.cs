using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PhotoBank.AccessControl;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Internal;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Services.Search;

public sealed class SearchReferenceDataService : ISearchReferenceDataService
{
    private readonly IRepository<Person> _personRepository;
    private readonly IRepository<Tag> _tagRepository;
    private readonly IRepository<Photo> _photoRepository;
    private readonly IRepository<Storage> _storageRepository;
    private readonly IRepository<PersonGroup> _personGroupRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IMemoryCache _cache;
    private readonly IMapper _mapper;

    private Lazy<Task<IReadOnlyList<PersonDto>>> _persons;
    private readonly Lazy<Task<IReadOnlyList<TagDto>>> _tags;
    private Lazy<Task<IReadOnlyList<PathDto>>> _paths;
    private Lazy<Task<IReadOnlyList<StorageDto>>> _storages;
    private Lazy<Task<IReadOnlyList<PersonGroupDto>>> _personGroups;

    public SearchReferenceDataService(
        IRepository<Person> personRepository,
        IRepository<Tag> tagRepository,
        IRepository<Photo> photoRepository,
        IRepository<Storage> storageRepository,
        IRepository<PersonGroup> personGroupRepository,
        ICurrentUser currentUser,
        IMemoryCache cache,
        IMapper mapper)
    {
        _personRepository = personRepository;
        _tagRepository = tagRepository;
        _photoRepository = photoRepository;
        _storageRepository = storageRepository;
        _personGroupRepository = personGroupRepository;
        _currentUser = currentUser;
        _cache = cache;
        _mapper = mapper;

        _persons = CreatePersonsLazy();
        _tags = CreateTagsLazy();
        _paths = CreatePathsLazy();
        _storages = CreateStoragesLazy();
        _personGroups = CreatePersonGroupsLazy();
    }

    public Task<IReadOnlyList<PersonDto>> GetPersonsAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyList<PersonDto>>(cancellationToken);
        }

        return _persons.Value;
    }

    public Task<IReadOnlyList<TagDto>> GetTagsAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyList<TagDto>>(cancellationToken);
        }

        return _tags.Value;
    }

    public Task<IReadOnlyList<PathDto>> GetPathsAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyList<PathDto>>(cancellationToken);
        }

        return _paths.Value;
    }

    public Task<IReadOnlyList<StorageDto>> GetStoragesAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyList<StorageDto>>(cancellationToken);
        }

        return _storages.Value;
    }

    public Task<IReadOnlyList<PersonGroupDto>> GetPersonGroupsAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyList<PersonGroupDto>>(cancellationToken);
        }

        return _personGroups.Value;
    }

    public void InvalidatePersons()
    {
        _cache.Remove(CacheKeys.PersonsAll);
        _cache.Remove(CacheKeys.PersonsOf(_currentUser.UserId));
        _persons = CreatePersonsLazy();
    }

    public void InvalidatePersonGroups()
    {
        _cache.Remove(CacheKeys.PersonGroups);
        _personGroups = CreatePersonGroupsLazy();
    }

    public void InvalidateStorages()
    {
        _cache.Remove(CacheKeys.StoragesAll);
        _cache.Remove(CacheKeys.StoragesOf(_currentUser.UserId));
        _cache.Remove(CacheKeys.PathsAll);
        _cache.Remove(CacheKeys.PathsOf(_currentUser.UserId));
        _storages = CreateStoragesLazy();
        _paths = CreatePathsLazy();
    }

    private Lazy<Task<IReadOnlyList<PersonDto>>> CreatePersonsLazy() => new(() =>
        _cache.GetOrCreateAsync(CacheKeys.Persons(_currentUser), async _ =>
        {
            var query = _personRepository.GetAll()
                .AsNoTracking()
                .MaybeApplyAcl(_currentUser);

            var items = await query
                .OrderBy(p => p.Name)
                .ThenBy(p => p.Id)
                .ProjectTo<PersonDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return (IReadOnlyList<PersonDto>)items;
        })!);

    private Lazy<Task<IReadOnlyList<TagDto>>> CreateTagsLazy() => new(() =>
        _cache.GetOrCreateAsync(CacheKeys.Tags, async _ =>
        {
            var items = await _tagRepository.GetAll()
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .ThenBy(t => t.Id)
                .ProjectTo<TagDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return (IReadOnlyList<TagDto>)items;
        })!);

    private Lazy<Task<IReadOnlyList<PathDto>>> CreatePathsLazy() => new(() =>
        _cache.GetOrCreateAsync(CacheKeys.Paths(_currentUser), async _ =>
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
        })!);

    private Lazy<Task<IReadOnlyList<StorageDto>>> CreateStoragesLazy() => new(() =>
        _cache.GetOrCreateAsync(CacheKeys.Storages(_currentUser), async _ =>
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
        })!);

    private Lazy<Task<IReadOnlyList<PersonGroupDto>>> CreatePersonGroupsLazy() => new(() =>
        _cache.GetOrCreateAsync(CacheKeys.PersonGroups, async _ =>
        {
            var items = await _personGroupRepository.GetAll()
                .AsNoTracking()
                .OrderBy(pg => pg.Name)
                .ThenBy(pg => pg.Id)
                .ProjectTo<PersonGroupDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            foreach (var group in items)
            {
                group.Persons ??= [];
            }

            return (IReadOnlyList<PersonGroupDto>)items;
        })!);
}
