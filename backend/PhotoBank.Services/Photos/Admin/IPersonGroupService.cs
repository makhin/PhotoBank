using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Internal;
using PhotoBank.Services.Search;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Services.Photos.Admin;

public interface IPersonGroupService
{
    Task<IEnumerable<PersonGroupDto>> GetAllPersonGroupsAsync();
    Task<PersonGroupDto> CreatePersonGroupAsync(string name);
    Task<PersonGroupDto> UpdatePersonGroupAsync(int groupId, string name);
    Task DeletePersonGroupAsync(int groupId);
    Task AddPersonToGroupAsync(int groupId, int personId);
    Task RemovePersonFromGroupAsync(int groupId, int personId);
}

public class PersonGroupService : IPersonGroupService
{
    private readonly PhotoBankDbContext _db;
    private readonly IRepository<PersonGroup> _personGroupRepository;
    private readonly IMapper _mapper;
    private readonly ISearchReferenceDataService _searchReferenceDataService;
    private readonly ILogger<PersonGroupService> _logger;

    public PersonGroupService(
        PhotoBankDbContext db,
        IRepository<PersonGroup> personGroupRepository,
        IMapper mapper,
        ISearchReferenceDataService searchReferenceDataService,
        ILogger<PersonGroupService> logger)
    {
        _db = db;
        _personGroupRepository = personGroupRepository;
        _mapper = mapper;
        _searchReferenceDataService = searchReferenceDataService;
        _logger = logger;
    }

    public async Task<IEnumerable<PersonGroupDto>> GetAllPersonGroupsAsync()
    {
        var groups = await _searchReferenceDataService.GetPersonGroupsAsync();
        return groups;
    }

    public async Task<PersonGroupDto> CreatePersonGroupAsync(string name)
    {
        var entity = await _personGroupRepository.InsertAsync(new PersonGroup { Name = name });
        InvalidateCache();
        return _mapper.Map<PersonGroupDto>(entity);
    }

    public async Task<PersonGroupDto> UpdatePersonGroupAsync(int groupId, string name)
    {
        var entity = new PersonGroup { Id = groupId, Name = name };
        await _personGroupRepository.UpdateAsync(entity, pg => pg.Name);
        InvalidateCache();
        return _mapper.Map<PersonGroupDto>(entity);
    }

    public async Task DeletePersonGroupAsync(int groupId)
    {
        await _personGroupRepository.DeleteAsync(groupId);
        InvalidateCache();
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
            InvalidateCache();
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
            InvalidateCache();
        }
    }

    private void InvalidateCache()
    {
        _logger.LogDebug("Invalidating person group cache");
        _searchReferenceDataService.InvalidatePersonGroups();

        // Also invalidate persons cache because non-admin users' person lists depend on person groups
        _searchReferenceDataService.InvalidatePersons();
    }
}
