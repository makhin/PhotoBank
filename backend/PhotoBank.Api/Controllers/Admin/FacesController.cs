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
    [ProducesResponseType(typeof(PageResponse<FaceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PageResponse<FaceDto>>> GetAllAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var faces = await photoService.GetFacesPageAsync(page, pageSize);
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
