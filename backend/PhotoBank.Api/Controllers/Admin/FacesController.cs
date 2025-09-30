using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoBank.Services.Api;
using PhotoBank.DbContext.Models;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Api.Controllers.Admin;

[Route("[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class FacesController(IPhotoService photoService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FaceIdentityDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FaceIdentityDto>>> GetAsync([FromQuery] IdentityStatus? status, [FromQuery] int? personId)
    {
        var faces = await photoService.GetFacesAsync(status, personId);
        return Ok(faces);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync([FromBody] UpdateFaceIdentityDto dto)
    {
        await photoService.UpdateFaceIdentityAsync(dto.FaceId, dto.IdentityStatus, dto.PersonId);
        return Ok();
    }

    [HttpGet("{id}/image")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status301MovedPermanently)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImage(int id)
    {
        var result = await photoService.GetFaceImageAsync(id);
        if (result is null)
            return NotFound();

        return CachedImageResponseBuilder.Build(this, result);
    }
}
