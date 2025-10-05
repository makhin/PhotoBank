using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PhotoBank.AccessControl;
using PhotoBank.ViewModel.Dto;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoBank.Api.Controllers.Admin;

[ApiController]
[Route("admin/access-profiles")]
[Authorize(Roles = "Admin")]
public class AdminAccessProfilesController : ControllerBase
{
    private readonly IAccessProfileService _profiles;

    public AdminAccessProfilesController(IAccessProfileService profiles)
    {
        _profiles = profiles;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AccessProfileDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AccessProfileDto>>> List(CancellationToken ct)
    {
        var profiles = await _profiles.ListAsync(ct);
        return Ok(profiles);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AccessProfileDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AccessProfileDto>> Create([FromBody] AccessProfileDto profile, CancellationToken ct)
    {
        var created = await _profiles.CreateAsync(profile, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AccessProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccessProfileDto>> Get(int id, CancellationToken ct)
    {
        var profile = await _profiles.GetAsync(id, ct);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Update(int id, [FromBody] AccessProfileDto dto, CancellationToken ct)
    {
        if (id != dto.Id) return BadRequest();
        var updated = await _profiles.UpdateAsync(id, dto, ct);
        if (!updated) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _profiles.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{id:int}/assign-user/{userId}")]
    public async Task<ActionResult> AssignUser(int id, string userId, CancellationToken ct)
    {
        var result = await _profiles.AssignUserAsync(id, userId, ct);
        return result ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}/assign-user/{userId}")]
    public async Task<ActionResult> UnassignUser(int id, string userId, CancellationToken ct)
    {
        var result = await _profiles.UnassignUserAsync(id, userId, ct);
        return result ? NoContent() : NotFound();
    }

    [HttpPost("{id:int}/assign-role/{roleId}")]
    public async Task<ActionResult> AssignRole(int id, string roleId, CancellationToken ct)
    {
        var result = await _profiles.AssignRoleAsync(id, roleId, ct);
        return result ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}/assign-role/{roleId}")]
    public async Task<ActionResult> UnassignRole(int id, string roleId, CancellationToken ct)
    {
        var result = await _profiles.UnassignRoleAsync(id, roleId, ct);
        return result ? NoContent() : NotFound();
    }
}
