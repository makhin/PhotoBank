using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PhotoBank.Services.Api;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Api.Controllers;

[Route("[controller]")]
[ApiController]
[Authorize]
public class PersonsController(IPhotoService photoService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PersonDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PersonDto>>> GetAllAsync()
    {
        var persons = await photoService.GetAllPersonsAsync();
        return Ok(persons);
    }
}