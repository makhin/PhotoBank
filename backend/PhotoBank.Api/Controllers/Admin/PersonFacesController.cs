using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoBank.Services.Api;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Api.Controllers.Admin;

[Route("[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class PersonFacesController(IPhotoService photoService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PersonFaceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PersonFaceDto>>> GetAllAsync()
    {
        var links = await photoService.GetAllPersonFacesAsync();
        return Ok(links);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PersonFaceDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PersonFaceDto>> CreateAsync(PersonFaceDto dto)
    {
        var link = await photoService.CreatePersonFaceAsync(dto);
        return CreatedAtAction(nameof(GetAllAsync), new { }, link);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(PersonFaceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PersonFaceDto>> UpdateAsync(int id, PersonFaceDto dto)
    {
        if (dto.Id != id)
            return BadRequest();
        var link = await photoService.UpdatePersonFaceAsync(id, dto);
        return Ok(link);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        await photoService.DeletePersonFaceAsync(id);
        return NoContent();
    }
}
