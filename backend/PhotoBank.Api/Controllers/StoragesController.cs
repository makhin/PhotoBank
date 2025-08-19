using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PhotoBank.Services.Api;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Api.Controllers;

[Route("[controller]")]
[ApiController]
[Authorize]
public class StoragesController(IPhotoService photoService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<StorageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<StorageDto>>> GetAllAsync()
    {
        var storages = await photoService.GetAllStoragesAsync();
        return Ok(storages);
    }
}