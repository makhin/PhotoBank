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

    private readonly CachedAsyncValue<IReadOnlyList<PersonDto>> _persons;
    private readonly CachedAsyncValue<IReadOnlyList<TagDto>> _tags;
    private readonly CachedAsyncValue<IReadOnlyList<PathDto>> _paths;
    private readonly CachedAsyncValue<IReadOnlyList<StorageDto>> _storages;
    private readonly CachedAsyncValue<IReadOnlyList<PersonGroupDto>> _personGroups;

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

        _persons = CreatePersonsCache();
        _tags = CreateTagsCache();
        _paths = CreatePathsCache();
        _storages = CreateStoragesCache();
        _personGroups = CreatePersonGroupsCache();
    }

    public Task<IReadOnlyList<PersonDto>> GetPersonsAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyList<PersonDto>>(cancellationToken);
        }

        return _persons.GetValueAsync();
    }

    public Task<IReadOnlyList<TagDto>> GetTagsAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyList<TagDto>>(cancellationToken);
        }

        return _tags.GetValueAsync();
    }

    public Task<IReadOnlyList<PathDto>> GetPathsAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyList<PathDto>>(cancellationToken);
        }

        return _paths.GetValueAsync();
    }

    public Task<IReadOnlyList<StorageDto>> GetStoragesAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyList<StorageDto>>(cancellationToken);
        }

        return _storages.GetValueAsync();
    }

    public Task<IReadOnlyList<PersonGroupDto>> GetPersonGroupsAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyList<PersonGroupDto>>(cancellationToken);
        }

        return _personGroups.GetValueAsync();
    }

    public void InvalidatePersons()
    {
        _persons.Reset();
    }

    public void InvalidatePersonGroups()
    {
        _personGroups.Reset();
    }

    public void InvalidateStorages()
    {
        _storages.Reset();
        _paths.Reset();
    }

    private CachedAsyncValue<T> CreateCachedValue<T>(
        Func<object> keyFactory,
        Func<ICacheEntry, Task<T>> valueFactory,
        Func<IEnumerable<object>>? invalidationKeysFactory = null)
        => new(_cache, keyFactory, valueFactory, invalidationKeysFactory);

    private CachedAsyncValue<IReadOnlyList<PersonDto>> CreatePersonsCache()
        => CreateCachedValue(
            () => CacheKeys.Persons(_currentUser),
            async _ =>
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
            },
            () => new object[]
            {
                CacheKeys.PersonsAll,
                CacheKeys.PersonsOf(_currentUser.UserId)
            });

    private CachedAsyncValue<IReadOnlyList<TagDto>> CreateTagsCache()
        => CreateCachedValue(
            () => CacheKeys.Tags,
            async _ =>
            {
                var items = await _tagRepository.GetAll()
                    .AsNoTracking()
                    .OrderBy(t => t.Name)
                    .ThenBy(t => t.Id)
                    .ProjectTo<TagDto>(_mapper.ConfigurationProvider)
                    .ToListAsync();

                return (IReadOnlyList<TagDto>)items;
            },
            () => new object[] { CacheKeys.Tags });

    private CachedAsyncValue<IReadOnlyList<PathDto>> CreatePathsCache()
        => CreateCachedValue(
            () => CacheKeys.Paths(_currentUser),
            async _ =>
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
                        Path = p.RelativePath!,
                    })
                    .ToList();
            },
            () => new object[]
            {
                CacheKeys.PathsAll,
                CacheKeys.PathsOf(_currentUser.UserId)
            });

    private CachedAsyncValue<IReadOnlyList<StorageDto>> CreateStoragesCache()
        => CreateCachedValue(
            () => CacheKeys.Storages(_currentUser),
            async _ =>
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
            },
            () => new object[]
            {
                CacheKeys.StoragesAll,
                CacheKeys.StoragesOf(_currentUser.UserId)
            });

    private CachedAsyncValue<IReadOnlyList<PersonGroupDto>> CreatePersonGroupsCache()
        => CreateCachedValue(
            () => CacheKeys.PersonGroups,
            async _ =>
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
            },
            () => new object[] { CacheKeys.PersonGroups });
}
