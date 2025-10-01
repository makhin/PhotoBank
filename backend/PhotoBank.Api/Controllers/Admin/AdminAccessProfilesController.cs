using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoBank.AccessControl;
using PhotoBank.ViewModel.Dto;
using System;
using System.Linq;

namespace PhotoBank.Api.Controllers.Admin;

[ApiController]
[Route("admin/access-profiles")]
[Authorize(Roles = "Admin")]
public class AdminAccessProfilesController : ControllerBase
{
    private readonly AccessControlDbContext _db;
    private readonly IEffectiveAccessProvider _eff;
    private readonly IMapper _mapper;

    public AdminAccessProfilesController(AccessControlDbContext db, IEffectiveAccessProvider eff, IMapper mapper)
    {
        _db = db;
        _eff = eff;
        _mapper = mapper;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AccessProfileDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AccessProfileDto>>> List(CancellationToken ct)
    {
        var profiles = await _db.AccessProfiles
            .AsNoTracking()
            .ProjectTo<AccessProfileDto>(_mapper.ConfigurationProvider)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        return Ok(profiles);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AccessProfileDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AccessProfileDto>> Create([FromBody] AccessProfileDto profile, CancellationToken ct)
    {
        var entity = _mapper.Map<AccessProfile>(profile);
        _db.AccessProfiles.Add(entity);
        await _db.SaveChangesAsync(ct);

        var created = await _db.AccessProfiles
            .AsNoTracking()
            .Where(x => x.Id == entity.Id)
            .ProjectTo<AccessProfileDto>(_mapper.ConfigurationProvider)
            .FirstAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AccessProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccessProfileDto>> Get(int id, CancellationToken ct)
    {
        var profile = await _db.AccessProfiles
            .AsNoTracking()
            .Where(x => x.Id == id)
            .ProjectTo<AccessProfileDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(ct);

        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Update(int id, [FromBody] AccessProfileDto dto, CancellationToken ct)
    {
        if (id != dto.Id) return BadRequest();
        var entity = await _db.AccessProfiles
            .Include(x => x.Storages)
            .Include(x => x.PersonGroups)
            .Include(x => x.DateRanges)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null) return NotFound();

        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.Flags_CanSeeNsfw = dto.Flags_CanSeeNsfw;

        UpdateStorages(entity, dto);
        UpdatePersonGroups(entity, dto);
        UpdateDateRanges(entity, dto);

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private void UpdateStorages(AccessProfile entity, AccessProfileDto dto)
    {
        var desired = (dto.Storages ?? Array.Empty<AccessProfileStorageAllowDto>())
            .Select(s => s.StorageId)
            .ToHashSet();

        var toRemove = entity.Storages
            .Where(s => !desired.Contains(s.StorageId))
            .ToList();

        foreach (var remove in toRemove)
        {
            _db.AccessProfileStorages.Remove(remove);
            entity.Storages.Remove(remove);
        }

        foreach (var storageId in desired)
        {
            if (entity.Storages.All(s => s.StorageId != storageId))
            {
                entity.Storages.Add(new AccessProfileStorageAllow
                {
                    ProfileId = entity.Id,
                    StorageId = storageId
                });
            }
        }
    }

    private void UpdatePersonGroups(AccessProfile entity, AccessProfileDto dto)
    {
        var desired = (dto.PersonGroups ?? Array.Empty<AccessProfilePersonGroupAllowDto>())
            .Select(pg => pg.PersonGroupId)
            .ToHashSet();

        var toRemove = entity.PersonGroups
            .Where(pg => !desired.Contains(pg.PersonGroupId))
            .ToList();

        foreach (var remove in toRemove)
        {
            _db.AccessProfilePersonGroups.Remove(remove);
            entity.PersonGroups.Remove(remove);
        }

        foreach (var personGroupId in desired)
        {
            if (entity.PersonGroups.All(pg => pg.PersonGroupId != personGroupId))
            {
                entity.PersonGroups.Add(new AccessProfilePersonGroupAllow
                {
                    ProfileId = entity.Id,
                    PersonGroupId = personGroupId
                });
            }
        }
    }

    private void UpdateDateRanges(AccessProfile entity, AccessProfileDto dto)
    {
        var desired = (dto.DateRanges ?? Array.Empty<AccessProfileDateRangeAllowDto>())
            .Select(r => (r.FromDate, r.ToDate))
            .ToHashSet();

        var toRemove = entity.DateRanges
            .Where(r => !desired.Contains((r.FromDate, r.ToDate)))
            .ToList();

        foreach (var remove in toRemove)
        {
            _db.AccessProfileDateRanges.Remove(remove);
            entity.DateRanges.Remove(remove);
        }

        foreach (var range in dto.DateRanges ?? Array.Empty<AccessProfileDateRangeAllowDto>())
        {
            if (entity.DateRanges.All(r => r.FromDate != range.FromDate || r.ToDate != range.ToDate))
            {
                entity.DateRanges.Add(new AccessProfileDateRangeAllow
                {
                    ProfileId = entity.Id,
                    FromDate = range.FromDate,
                    ToDate = range.ToDate
                });
            }
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
    {
        var p = await _db.AccessProfiles.FindAsync([id], ct);
        if (p is null) return NotFound();
        _db.Remove(p);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:int}/assign-user/{userId}")]
    public async Task<ActionResult> AssignUser(int id, string userId, CancellationToken ct)
    {
        var exists = await _db.AccessProfiles.AnyAsync(x => x.Id == id, ct);
        if (!exists) return NotFound();

        if (!await _db.UserAccessProfiles.AnyAsync(x => x.UserId == userId && x.ProfileId == id, ct))
        {
            _db.UserAccessProfiles.Add(new UserAccessProfile { UserId = userId, ProfileId = id });
            await _db.SaveChangesAsync(ct);
        }

        _eff.Invalidate(userId);
        return NoContent();
    }

    [HttpDelete("{id:int}/assign-user/{userId}")]
    public async Task<ActionResult> UnassignUser(int id, string userId, CancellationToken ct)
    {
        var link = await _db.UserAccessProfiles.FirstOrDefaultAsync(x => x.ProfileId == id && x.UserId == userId, ct);
        if (link is null) return NotFound();
        _db.UserAccessProfiles.Remove(link);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:int}/assign-role/{roleId}")]
    public async Task<ActionResult> AssignRole(int id, string roleId, CancellationToken ct)
    {
        var exists = await _db.AccessProfiles.AnyAsync(x => x.Id == id, ct);
        if (!exists) return NotFound();

        if (!await _db.RoleAccessProfiles.AnyAsync(x => x.RoleId == roleId && x.ProfileId == id, ct))
        {
            _db.RoleAccessProfiles.Add(new RoleAccessProfile { RoleId = roleId, ProfileId = id });
            await _db.SaveChangesAsync(ct);
        }

        return NoContent();
    }

    [HttpDelete("{id:int}/assign-role/{roleId}")]
    public async Task<ActionResult> UnassignRole(int id, string roleId, CancellationToken ct)
    {
        var link = await _db.RoleAccessProfiles.FirstOrDefaultAsync(x => x.ProfileId == id && x.RoleId == roleId, ct);
        if (link is null) return NotFound();
        _db.RoleAccessProfiles.Remove(link);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
