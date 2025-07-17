using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PhotoBank.Services.Api;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TagsController(IPhotoService photoService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TagDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TagDto>>> GetAllAsync()
    {
        var tags = await photoService.GetAllTagsAsync();
        return Ok(tags);
    }
}