using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PhotoBank.Services.Api;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Api.Controllers;

[Route("[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class PersonsController(IPhotoService photoService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PersonDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PersonDto>>> GetAllAsync()
    {
        var persons = await photoService.GetAllPersonsAsync();
        return Ok(persons);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PersonDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PersonDto>> CreateAsync(PersonDto dto)
    {
        var person = await photoService.CreatePersonAsync(dto.Name);
        return CreatedAtAction(nameof(GetAllAsync), new { }, person);
    }

    [HttpPut("{personId}")]
    [ProducesResponseType(typeof(PersonDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PersonDto>> UpdateAsync(int personId, PersonDto dto)
    {
        if (dto.Id != personId)
            return BadRequest();
        var person = await photoService.UpdatePersonAsync(personId, dto.Name);
        return Ok(person);
    }

    [HttpDelete("{personId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(int personId)
    {
        await photoService.DeletePersonAsync(personId);
        return NoContent();
    }
}