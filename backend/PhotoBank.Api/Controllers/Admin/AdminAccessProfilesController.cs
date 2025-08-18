using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoBank.AccessControl;

namespace PhotoBank.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/access-profiles")]
[Authorize(Roles = "Admin")]
public class AdminAccessProfilesController : ControllerBase
{
    private readonly AccessControlDbContext _db;
    private readonly IEffectiveAccessProvider _eff;

    public AdminAccessProfilesController(AccessControlDbContext db, IEffectiveAccessProvider eff)
    {
        _db = db; _eff = eff;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AccessProfile>>> List(CancellationToken ct)
        => Ok(await _db.AccessProfiles
            .Include(p => p.Storages)
            .Include(p => p.PersonGroups)
            .Include(p => p.DateRanges)
            .OrderBy(p => p.Name)
            .ToListAsync(ct));

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] AccessProfile profile, CancellationToken ct)
    {
        _db.AccessProfiles.Add(profile);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = profile.Id }, profile);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AccessProfile>> Get(int id, CancellationToken ct)
    {
        var p = await _db.AccessProfiles
            .Include(x => x.Storages)
            .Include(x => x.PersonGroups)
            .Include(x => x.DateRanges)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return p is null ? NotFound() : Ok(p);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] AccessProfile dto, CancellationToken ct)
    {
        if (id != dto.Id) return BadRequest();
        _db.Entry(dto).State = EntityState.Modified;
        await _db.SaveChangesAsync(ct);
        return NoContent();
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
        var rel = await _db.UserAccessProfiles.FirstOrDefaultAsync(x => x.UserId == userId && x.ProfileId == id, ct);
        if (rel is null) return NotFound();

        _db.UserAccessProfiles.Remove(rel);
        await _db.SaveChangesAsync(ct);
        _eff.Invalidate(userId);
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
}
