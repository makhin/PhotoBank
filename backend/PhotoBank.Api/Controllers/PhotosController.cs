using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoBank.Services.Api;
using PhotoBank.Services.Search;
using PhotoBank.ViewModel.Dto;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;

namespace PhotoBank.Api.Controllers
{
    [Route("[controller]")]
    [Authorize]
    [ApiController]
    public class PhotosController(
        ILogger<PhotosController> logger,
        IPhotoService photoService,
        ISearchFilterNormalizer normalizer)
        : ControllerBase
    {
        [HttpPost("search")]
        [ProducesResponseType(typeof(PageResponse<PhotoItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PageResponse<PhotoItemDto>>> SearchPhotos([FromBody] FilterDto request)
        {
            logger.LogInformation("Searching photos with filter {@Filter}", request);
            await normalizer.NormalizeAsync(request);
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

        [HttpGet("{id}/preview")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status301MovedPermanently)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPreview(int id)
        {
            logger.LogInformation("Fetching preview for photo {Id}", id);
            var result = await photoService.GetPhotoPreviewAsync(id);
            if (result is null)
            {
                logger.LogWarning("Preview for photo {Id} not found", id);
                return NotFound();
            }

            var etag = $"\"{result.ETag}\"";
            Response.Headers.ETag = etag;
            Response.Headers.CacheControl = "public, max-age=31536000, immutable";

            if (Request.Headers.IfNoneMatch.Contains(etag))
            {
                logger.LogInformation("Preview for photo {Id} not modified", id);
                return StatusCode(StatusCodes.Status304NotModified);
            }

            if (result.PreSignedUrl is not null)
            {
                logger.LogInformation("Redirecting to pre-signed URL for photo {Id}", id);
                Response.Headers.Location = result.PreSignedUrl;
                return StatusCode(StatusCodes.Status301MovedPermanently);
            }

            logger.LogInformation("Streaming preview for photo {Id}", id);
            return File(result.Data!, MediaTypeNames.Image.Jpeg);
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
