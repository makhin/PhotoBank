using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Search;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Services.Photos.Admin;

public interface IPersonDirectoryService
{
    Task<IEnumerable<PersonDto>> GetAllPersonsAsync();
    Task<PersonDto> CreatePersonAsync(string name);
    Task<PersonDto> UpdatePersonAsync(int personId, string name);
    Task DeletePersonAsync(int personId);
}

public class PersonDirectoryService : IPersonDirectoryService
{
    private readonly IRepository<Person> _personRepository;
    private readonly IMapper _mapper;
    private readonly ISearchReferenceDataService _searchReferenceDataService;
    private readonly ILogger<PersonDirectoryService> _logger;

    public PersonDirectoryService(
        IRepository<Person> personRepository,
        IMapper mapper,
        ISearchReferenceDataService searchReferenceDataService,
        ILogger<PersonDirectoryService> logger)
    {
        _personRepository = personRepository;
        _mapper = mapper;
        _searchReferenceDataService = searchReferenceDataService;
        _logger = logger;
    }

    public async Task<IEnumerable<PersonDto>> GetAllPersonsAsync()
    {
        var persons = await _searchReferenceDataService.GetPersonsAsync();
        return persons;
    }

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

    private void InvalidatePersonsCache()
    {
        _logger.LogDebug("Invalidating persons cache");
        _searchReferenceDataService.InvalidatePersons();
    }
}
