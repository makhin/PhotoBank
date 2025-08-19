using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoBank.Services.Api;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Api.Controllers;

[Route("[controller]")]
[ApiController]
[Authorize]
public class FacesController(IPhotoService photoService) : ControllerBase
{
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync([FromBody] UpdateFaceDto dto)
    {
        await photoService.UpdateFaceAsync(dto.FaceId, dto.PersonId);
        return Ok();
    }
}
