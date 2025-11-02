using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using PhotoBank.Services.Internal;
using System.Web;

namespace PhotoBank.Api.Controllers;

[Route("[controller]")]
[Authorize]
[ApiController]
public class MediaController : ControllerBase
{
    private readonly IMinioClient _minioClient;
    private readonly ILogger<MediaController> _logger;
    private readonly S3Options _s3Options;

    public MediaController(
        IMinioClient minioClient,
        ILogger<MediaController> logger,
        IOptions<S3Options> s3Options)
    {
        _minioClient = minioClient ?? throw new ArgumentNullException(nameof(minioClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _s3Options = s3Options?.Value ?? throw new ArgumentNullException(nameof(s3Options));
    }

    /// <summary>
    /// Proxies an image from S3 storage.
    /// </summary>
    /// <param name="key">The S3 object key (URL-encoded)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The image file</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMedia([FromQuery] string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogWarning("Media request with empty key");
            return BadRequest("Key parameter is required");
        }

        try
        {
            // Decode the key in case it's URL-encoded
            var decodedKey = HttpUtility.UrlDecode(key);

            _logger.LogDebug("Fetching media object: {Key}", decodedKey);

            // Get the object from S3/MinIO
            var statArgs = new StatObjectArgs()
                .WithBucket(_s3Options.Bucket)
                .WithObject(decodedKey);

            var stat = await _minioClient.StatObjectAsync(statArgs, cancellationToken);

            var getArgs = new GetObjectArgs()
                .WithBucket(_s3Options.Bucket)
                .WithObject(decodedKey)
                .WithCallbackStream(async (stream, ct) =>
                {
                    await stream.CopyToAsync(Response.Body, ct);
                });

            // Determine content type from the object metadata or file extension
            var contentType = stat.ContentType ?? GetContentTypeFromExtension(decodedKey);

            Response.Headers.ContentType = contentType;
            Response.Headers.ContentLength = stat.Size;
            Response.Headers.CacheControl = "public, max-age=31536000"; // Cache for 1 year
            Response.Headers["ETag"] = stat.ETag;

            await _minioClient.GetObjectAsync(getArgs, cancellationToken);

            _logger.LogDebug("Successfully served media object: {Key}, Size: {Size}", decodedKey, stat.Size);
            return new EmptyResult();
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            _logger.LogWarning("Media object not found: {Key}", key);
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching media object: {Key}", key);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error fetching media");
        }
    }

    private static string GetContentTypeFromExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            ".tiff" or ".tif" => "image/tiff",
            _ => "application/octet-stream"
        };
    }
}
