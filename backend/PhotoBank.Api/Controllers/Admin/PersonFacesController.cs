using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoBank.Services.Api;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Api.Controllers.Admin;

[Route("personfaces")]
[ApiController]
[Authorize(Roles = "Admin")]
public class PersonFacesController(IPhotoService photoService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FaceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FaceDto>>> GetAllAsync()
    {
        var faces = await photoService.GetFacesMetadataAsync();
        return Ok(faces);
    }

    [HttpPost]
    [ProducesResponseType(typeof(FaceDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<FaceDto>> CreateAsync([FromBody] FaceDto dto)
    {
        var face = await photoService.CreateFaceMetadataAsync(dto);
        return CreatedAtAction(nameof(GetAllAsync), new { id = face.Id }, face);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(FaceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<FaceDto>> UpdateAsync(int id, [FromBody] FaceDto dto)
    {
        if (dto.Id != id)
            return BadRequest();

        var face = await photoService.UpdateFaceMetadataAsync(id, dto);
        return Ok(face);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        await photoService.DeleteFaceMetadataAsync(id);
        return NoContent();
    }
}
