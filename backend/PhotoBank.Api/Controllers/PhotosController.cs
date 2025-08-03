using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoBank.Services.Api;
using PhotoBank.ViewModel.Dto;
using System.Diagnostics.Metrics;
using System.Collections.Generic;

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

        [HttpPost("upload")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Upload([FromForm] List<IFormFile> files, [FromForm] int storageId, [FromForm] string path)
        {
            await photoService.UploadPhotosAsync(files, storageId, path);
            return Ok();
        }

        [HttpGet("duplicates")]
        [ProducesResponseType(typeof(IEnumerable<PhotoItemDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<PhotoItemDto>>> GetDuplicates([FromQuery] int? id, [FromQuery] string? hash, [FromQuery] int threshold = 5)
        {
            var result = await photoService.FindDuplicatesAsync(id, hash, threshold);
            return Ok(result);
        }
    }
}
