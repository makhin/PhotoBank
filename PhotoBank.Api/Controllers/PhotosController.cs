using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoBank.Services.Api;
using PhotoBank.ViewModel.Dto;
using System.Diagnostics.Metrics;

namespace PhotoBank.Api.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class PhotosController(ILogger<PhotosController> logger, IPhotoService photoService)
        : ControllerBase
    {
        [HttpPost("search")]
        [ProducesResponseType(typeof(QueryResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<QueryResult>> SearchPhotos([FromBody] FilterDto request)
        {
            var result = await photoService.GetAllPhotosAsync(request);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PhotoDto>> GetPhoto(int id)
        {
            var photo = await photoService.GetPhotoAsync(id);
            if (photo == null)
                return NotFound();

            return Ok(photo);
        }
    }
}
