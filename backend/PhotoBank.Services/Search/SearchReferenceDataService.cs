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
    private readonly ICurrentUser _currentUser;
    private readonly IMemoryCache _cache;
    private readonly IMapper _mapper;

    private readonly Lazy<Task<IReadOnlyList<PersonDto>>> _persons;
    private readonly Lazy<Task<IReadOnlyList<TagDto>>> _tags;

    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpiration = null,
        SlidingExpiration = null
    };

    public SearchReferenceDataService(
        IRepository<Person> personRepository,
        IRepository<Tag> tagRepository,
        ICurrentUser currentUser,
        IMemoryCache cache,
        IMapper mapper)
    {
        _personRepository = personRepository;
        _tagRepository = tagRepository;
        _currentUser = currentUser;
        _cache = cache;
        _mapper = mapper;

        _persons = new Lazy<Task<IReadOnlyList<PersonDto>>>(() =>
            GetCachedAsync(CacheKeys.Persons(_currentUser), async () =>
            {
                var query = _personRepository.GetAll()
                    .AsNoTracking()
                    .MaybeApplyAcl(_currentUser);

                var items = await query
                    .OrderBy(p => p.Name).ThenBy(p => p.Id)
                    .ProjectTo<PersonDto>(_mapper.ConfigurationProvider)
                    .ToListAsync();
                return (IReadOnlyList<PersonDto>)items;
            }));

        _tags = new Lazy<Task<IReadOnlyList<TagDto>>>(() =>
            GetCachedAsync(CacheKeys.Tags, async () =>
            {
                var items = await _tagRepository.GetAll()
                    .AsNoTracking()
                    .OrderBy(t => t.Name).ThenBy(t => t.Id)
                    .ProjectTo<TagDto>(_mapper.ConfigurationProvider)
                    .ToListAsync();
                return (IReadOnlyList<TagDto>)items;
            }));
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

    public void InvalidatePersonsCache()
    {
        _cache.Remove(CacheKeys.PersonsAll);
        _cache.Remove(CacheKeys.PersonsOf(_currentUser.UserId));
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
}
