using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoBank.Services.Api;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Api.Controllers.Admin;

[Route("[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class FacesController(IPhotoService photoService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FaceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FaceDto>>> GetAllAsync()
    {
        var faces = await photoService.GetAllFacesAsync();
        return Ok(faces);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync([FromBody] FaceDto dto)
    {
        await photoService.UpdateFaceAsync(dto.Id, dto.PersonId);
        return Ok();
    }
}
