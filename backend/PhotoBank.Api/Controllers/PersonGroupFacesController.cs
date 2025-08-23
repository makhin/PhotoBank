using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoBank.Services.Api;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Api.Controllers;

[Route("[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class PersonGroupFacesController(IPhotoService photoService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PersonGroupFaceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PersonGroupFaceDto>>> GetAllAsync()
    {
        var links = await photoService.GetAllPersonGroupFacesAsync();
        return Ok(links);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PersonGroupFaceDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PersonGroupFaceDto>> CreateAsync(PersonGroupFaceDto dto)
    {
        var link = await photoService.CreatePersonGroupFaceAsync(dto);
        return CreatedAtAction(nameof(GetAllAsync), new { }, link);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(PersonGroupFaceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PersonGroupFaceDto>> UpdateAsync(int id, PersonGroupFaceDto dto)
    {
        if (dto.Id != id)
            return BadRequest();
        var link = await photoService.UpdatePersonGroupFaceAsync(id, dto);
        return Ok(link);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        await photoService.DeletePersonGroupFaceAsync(id);
        return NoContent();
    }
}
