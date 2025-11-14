using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.AccessControl;

public sealed class AccessProfileService : IAccessProfileService
{
    private readonly AccessControlDbContext _db;
    private readonly IMapper _mapper;
    private readonly IEffectiveAccessProvider _effectiveAccessProvider;

    public AccessProfileService(
        AccessControlDbContext db,
        IMapper mapper,
        IEffectiveAccessProvider effectiveAccessProvider)
    {
        _db = db;
        _mapper = mapper;
        _effectiveAccessProvider = effectiveAccessProvider;
    }

    public async Task<IReadOnlyList<AccessProfileDto>> ListAsync(CancellationToken ct)
    {
        return await _db.AccessProfiles
            .AsNoTracking()
            .ProjectTo<AccessProfileDto>(_mapper.ConfigurationProvider)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
    }

    public async Task<AccessProfileDto?> GetAsync(int id, CancellationToken ct)
    {
        return await _db.AccessProfiles
            .AsNoTracking()
            .Where(x => x.Id == id)
            .ProjectTo<AccessProfileDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<AccessProfileDto> CreateAsync(AccessProfileDto dto, CancellationToken ct)
    {
        var entity = _mapper.Map<AccessProfile>(dto);
        _db.AccessProfiles.Add(entity);
        await _db.SaveChangesAsync(ct);

        return await _db.AccessProfiles
            .AsNoTracking()
            .Where(x => x.Id == entity.Id)
            .ProjectTo<AccessProfileDto>(_mapper.ConfigurationProvider)
            .FirstAsync(ct);
    }

    public async Task<bool> UpdateAsync(int id, AccessProfileDto dto, CancellationToken ct)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var entity = await _db.AccessProfiles
            .Include(x => x.Storages)
            .Include(x => x.PersonGroups)
            .Include(x => x.DateRanges)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
        {
            return false;
        }

        var affectedUsers = await _db.UserAccessProfiles
            .Where(x => x.ProfileId == id)
            .Select(x => x.UserId)
            .ToListAsync(ct);

        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.Flags_CanSeeNsfw = dto.Flags_CanSeeNsfw;

        SyncStorages(entity, dto);
        SyncPersonGroups(entity, dto);
        SyncDateRanges(entity, dto);

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        InvalidateUsers(affectedUsers);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var entity = await _db.AccessProfiles.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return false;
        }

        var affectedUsers = await _db.UserAccessProfiles
            .Where(x => x.ProfileId == id)
            .Select(x => x.UserId)
            .ToListAsync(ct);

        _db.AccessProfiles.Remove(entity);
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        InvalidateUsers(affectedUsers);
        return true;
    }

    public async Task<bool> AssignUserAsync(int profileId, Guid userId, CancellationToken ct)
    {
        var profileExists = await _db.AccessProfiles.AnyAsync(x => x.Id == profileId, ct);
        if (!profileExists)
        {
            return false;
        }

        var alreadyAssigned = await _db.UserAccessProfiles
            .AnyAsync(x => x.ProfileId == profileId && x.UserId == userId, ct);
        if (!alreadyAssigned)
        {
            _db.UserAccessProfiles.Add(new UserAccessProfile
            {
                ProfileId = profileId,
                UserId = userId
            });
            await _db.SaveChangesAsync(ct);
            _effectiveAccessProvider.Invalidate(userId.ToString());
        }

        return true;
    }

    public async Task<bool> UnassignUserAsync(int profileId, Guid userId, CancellationToken ct)
    {
        var link = await _db.UserAccessProfiles
            .FirstOrDefaultAsync(x => x.ProfileId == profileId && x.UserId == userId, ct);
        if (link is null)
        {
            return false;
        }

        _db.UserAccessProfiles.Remove(link);
        await _db.SaveChangesAsync(ct);
        _effectiveAccessProvider.Invalidate(userId.ToString());
        return true;
    }

    public async Task<bool> AssignRoleAsync(int profileId, Guid roleId, CancellationToken ct)
    {
        var profileExists = await _db.AccessProfiles.AnyAsync(x => x.Id == profileId, ct);
        if (!profileExists)
        {
            return false;
        }

        var alreadyAssigned = await _db.RoleAccessProfiles
            .AnyAsync(x => x.ProfileId == profileId && x.RoleId == roleId, ct);
        if (!alreadyAssigned)
        {
            _db.RoleAccessProfiles.Add(new RoleAccessProfile
            {
                ProfileId = profileId,
                RoleId = roleId
            });
            await _db.SaveChangesAsync(ct);
        }

        return true;
    }

    public async Task<bool> UnassignRoleAsync(int profileId, Guid roleId, CancellationToken ct)
    {
        var link = await _db.RoleAccessProfiles
            .FirstOrDefaultAsync(x => x.ProfileId == profileId && x.RoleId == roleId, ct);
        if (link is null)
        {
            return false;
        }

        _db.RoleAccessProfiles.Remove(link);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private void SyncStorages(AccessProfile entity, AccessProfileDto dto)
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

    private void SyncPersonGroups(AccessProfile entity, AccessProfileDto dto)
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

    private void SyncDateRanges(AccessProfile entity, AccessProfileDto dto)
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

    private void InvalidateUsers(IEnumerable<Guid> userIds)
    {
        foreach (var userId in userIds.Distinct())
        {
            _effectiveAccessProvider.Invalidate(userId.ToString());
        }
    }
}
