using Microsoft.AspNetCore.Mvc;
using PhotoBank.Services.Api;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PathsController(IPhotoService photoService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PathDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PathDto>>> GetAllAsync()
    {
        var paths = await photoService.GetAllPathsAsync();
        return Ok(paths);
    }
}
