using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoBank.Services.Api;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Api.Controllers;

[Route("[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class PersonGroupsController(IPhotoService photoService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PersonGroupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PersonGroupDto>>> GetAllAsync()
    {
        var groups = await photoService.GetAllPersonGroupsAsync();
        return Ok(groups);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PersonGroupDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PersonGroupDto>> CreateAsync(PersonGroupDto dto)
    {
        var group = await photoService.CreatePersonGroupAsync(dto.Name);
        return CreatedAtAction(nameof(GetAllAsync), new { }, group);
    }

    [HttpPut("{groupId}")]
    [ProducesResponseType(typeof(PersonGroupDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PersonGroupDto>> UpdateAsync(int groupId, PersonGroupDto dto)
    {
        if (dto.Id != groupId)
            return BadRequest();
        var group = await photoService.UpdatePersonGroupAsync(groupId, dto.Name);
        return Ok(group);
    }

    [HttpDelete("{groupId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(int groupId)
    {
        await photoService.DeletePersonGroupAsync(groupId);
        return NoContent();
    }

    [HttpPost("{groupId}/persons/{personId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AddPersonAsync(int groupId, int personId)
    {
        await photoService.AddPersonToGroupAsync(groupId, personId);
        return NoContent();
    }

    [HttpDelete("{groupId}/persons/{personId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemovePersonAsync(int groupId, int personId)
    {
        await photoService.RemovePersonFromGroupAsync(groupId, personId);
        return NoContent();
    }
}

