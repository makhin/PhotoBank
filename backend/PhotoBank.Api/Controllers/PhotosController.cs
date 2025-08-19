using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoBank.Services.Api;
using PhotoBank.ViewModel.Dto;
using System.Collections.Generic;
using System.Linq;

namespace PhotoBank.Api.Controllers
{
    [Route("[controller]")]
    [Authorize]
    [ApiController]
    public class PhotosController(ILogger<PhotosController> logger, IPhotoService photoService)
        : ControllerBase
    {
        [HttpPost("search")]
        [ProducesResponseType(typeof(PageResponse<PhotoItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PageResponse<PhotoItemDto>>> SearchPhotos([FromBody] FilterDto request)
        {
            logger.LogInformation("Searching photos with filter {@Filter}", request);
            var result = await photoService.GetAllPhotosAsync(request);
            logger.LogInformation("Found {Count} photos", result.TotalCount);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PhotoDto>> GetPhoto(int id)
        {
            logger.LogInformation("Fetching photo with id {Id}", id);
            var photo = await photoService.GetPhotoAsync(id);
            if (photo == null)
            {
                logger.LogWarning("Photo with id {Id} not found", id);
                return NotFound();
            }

            logger.LogInformation("Returning photo with id {Id}", id);
            return Ok(photo);
        }

        [HttpPost("upload")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Upload([FromForm] List<IFormFile> files, [FromForm] int storageId, [FromForm] string path)
        {
            logger.LogInformation("Uploading {Count} files to storage {StorageId} at {Path}", files.Count, storageId, path);
            await photoService.UploadPhotosAsync(files, storageId, path);
            logger.LogInformation("Uploaded {Count} files", files.Count);
            return Ok();
        }

        [HttpGet("duplicates")]
        [ProducesResponseType(typeof(IEnumerable<PhotoItemDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<PhotoItemDto>>> GetDuplicates([FromQuery] int? id, [FromQuery] string? hash, [FromQuery] int threshold = 5)
        {
            logger.LogInformation("Searching duplicates for id {Id} hash {Hash} with threshold {Threshold}", id, hash, threshold);
            var result = await photoService.FindDuplicatesAsync(id, hash, threshold);
            logger.LogInformation("Found {Count} duplicates", result.Count());
            return Ok(result);
        }
    }
}
