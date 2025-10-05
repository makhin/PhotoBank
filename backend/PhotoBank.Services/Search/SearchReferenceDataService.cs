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
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IMemoryCache _cache;
    private readonly IMapper _mapper;

    private CachedAsyncValue<IReadOnlyList<PersonDto>>? _persons;
    private CachedAsyncValue<IReadOnlyList<TagDto>>? _tags;
    private CachedAsyncValue<IReadOnlyList<PathDto>>? _paths;
    private CachedAsyncValue<IReadOnlyList<StorageDto>>? _storages;
    private CachedAsyncValue<IReadOnlyList<PersonGroupDto>>? _personGroups;
    private ICurrentUser? _currentUser;

    public SearchReferenceDataService(
        IRepository<Person> personRepository,
        IRepository<Tag> tagRepository,
        IRepository<Photo> photoRepository,
        IRepository<Storage> storageRepository,
        IRepository<PersonGroup> personGroupRepository,
        ICurrentUserAccessor currentUserAccessor,
        IMemoryCache cache,
        IMapper mapper)
    {
        _personRepository = personRepository;
        _tagRepository = tagRepository;
        _photoRepository = photoRepository;
        _storageRepository = storageRepository;
        _personGroupRepository = personGroupRepository;
        _currentUserAccessor = currentUserAccessor;
        _cache = cache;
        _mapper = mapper;
    }

    public Task<IReadOnlyList<PersonDto>> GetPersonsAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyList<PersonDto>>(cancellationToken);
        }

        return GetOrCreatePersonsCacheAsync(cancellationToken);
    }

    public Task<IReadOnlyList<TagDto>> GetTagsAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyList<TagDto>>(cancellationToken);
        }

        return GetOrCreateTagsCacheAsync(cancellationToken);
    }

    public Task<IReadOnlyList<PathDto>> GetPathsAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyList<PathDto>>(cancellationToken);
        }

        return GetOrCreatePathsCacheAsync(cancellationToken);
    }

    public Task<IReadOnlyList<StorageDto>> GetStoragesAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyList<StorageDto>>(cancellationToken);
        }

        return GetOrCreateStoragesCacheAsync(cancellationToken);
    }

    public Task<IReadOnlyList<PersonGroupDto>> GetPersonGroupsAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyList<PersonGroupDto>>(cancellationToken);
        }

        return GetOrCreatePersonGroupsCacheAsync(cancellationToken);
    }

    public void InvalidatePersons()
    {
        _persons?.Reset();
    }

    public void InvalidatePersonGroups()
    {
        _personGroups?.Reset();
    }

    public void InvalidateStorages()
    {
        _storages?.Reset();
        _paths?.Reset();
    }

    private CachedAsyncValue<T> CreateCachedValue<T>(
        Func<object> keyFactory,
        Func<ICacheEntry, Task<T>> valueFactory,
        Func<IEnumerable<object>>? invalidationKeysFactory = null)
        => new(_cache, keyFactory, valueFactory, invalidationKeysFactory);

    private CachedAsyncValue<IReadOnlyList<PersonDto>> CreatePersonsCache(ICurrentUser currentUser)
        => CreateCachedValue(
            () => CacheKeys.Persons(currentUser),
            async _ =>
            {
                var query = _personRepository.GetAll()
                    .AsNoTracking()
                    .MaybeApplyAcl(currentUser);

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
                CacheKeys.PersonsOf(currentUser.UserId)
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

    private CachedAsyncValue<IReadOnlyList<PathDto>> CreatePathsCache(ICurrentUser currentUser)
        => CreateCachedValue(
            () => CacheKeys.Paths(currentUser),
            async _ =>
            {
                var query = _photoRepository.GetAll()
                    .AsNoTracking()
                    .MaybeApplyAcl(currentUser)
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
                CacheKeys.PathsOf(currentUser.UserId)
            });

    private CachedAsyncValue<IReadOnlyList<StorageDto>> CreateStoragesCache(ICurrentUser currentUser)
        => CreateCachedValue(
            () => CacheKeys.Storages(currentUser),
            async _ =>
            {
                var query = _storageRepository.GetAll()
                    .AsNoTracking()
                    .MaybeApplyAcl(currentUser);

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
                CacheKeys.StoragesOf(currentUser.UserId)
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

    private async Task<IReadOnlyList<PersonDto>> GetOrCreatePersonsCacheAsync(CancellationToken cancellationToken)
    {
        var user = await EnsureCurrentUserAsync(cancellationToken).ConfigureAwait(false);
        _persons ??= CreatePersonsCache(user);
        return await _persons.GetValueAsync().ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<TagDto>> GetOrCreateTagsCacheAsync(CancellationToken cancellationToken)
    {
        await EnsureCurrentUserAsync(cancellationToken).ConfigureAwait(false);
        _tags ??= CreateTagsCache();
        return await _tags.GetValueAsync().ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<PathDto>> GetOrCreatePathsCacheAsync(CancellationToken cancellationToken)
    {
        var user = await EnsureCurrentUserAsync(cancellationToken).ConfigureAwait(false);
        _paths ??= CreatePathsCache(user);
        return await _paths.GetValueAsync().ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<StorageDto>> GetOrCreateStoragesCacheAsync(CancellationToken cancellationToken)
    {
        var user = await EnsureCurrentUserAsync(cancellationToken).ConfigureAwait(false);
        _storages ??= CreateStoragesCache(user);
        return await _storages.GetValueAsync().ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<PersonGroupDto>> GetOrCreatePersonGroupsCacheAsync(CancellationToken cancellationToken)
    {
        await EnsureCurrentUserAsync(cancellationToken).ConfigureAwait(false);
        _personGroups ??= CreatePersonGroupsCache();
        return await _personGroups.GetValueAsync().ConfigureAwait(false);
    }

    private async Task<ICurrentUser> EnsureCurrentUserAsync(CancellationToken cancellationToken)
    {
        if (_currentUser is not null)
        {
            return _currentUser;
        }

        var resolved = await _currentUserAccessor.GetCurrentUserAsync(cancellationToken).ConfigureAwait(false);
        _currentUser = resolved;
        return resolved;
    }
}
