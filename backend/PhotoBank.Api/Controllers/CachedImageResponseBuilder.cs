using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PhotoBank.Services.Api;
using System.Net.Mime;

namespace PhotoBank.Api.Controllers;

public static class CachedImageResponseBuilder
{
    private const string CacheControlValue = "public, max-age=31536000, immutable";

    public static IActionResult Build(
        ControllerBase controller,
        PhotoPreviewResult result,
        ILogger? logger = null,
        CachedImageResponseCallbacks? callbacks = null)
    {
        var etag = $"\"{result.ETag}\"";
        controller.Response.Headers.ETag = etag;
        controller.Response.Headers.CacheControl = CacheControlValue;

        if (controller.Request.Headers.IfNoneMatch.Contains(etag))
        {
            if (callbacks?.OnNotModified is not null)
            {
                callbacks.OnNotModified();
            }
            else
            {
                logger?.LogInformation("Cached image not modified");
            }

            return controller.StatusCode(StatusCodes.Status304NotModified);
        }

        if (!string.IsNullOrEmpty(result.PreSignedUrl))
        {
            controller.Response.Headers.Location = result.PreSignedUrl;

            if (callbacks?.OnRedirect is not null)
            {
                callbacks.OnRedirect();
            }
            else
            {
                logger?.LogInformation("Redirecting to cached image pre-signed URL");
            }

            return controller.StatusCode(StatusCodes.Status301MovedPermanently);
        }

        if (callbacks?.OnStream is not null)
        {
            callbacks.OnStream();
        }
        else
        {
            logger?.LogInformation("Streaming cached image content");
        }

        return controller.File(result.Data!, MediaTypeNames.Image.Jpeg);
    }
}

public record CachedImageResponseCallbacks(Action? OnNotModified = null, Action? OnRedirect = null, Action? OnStream = null);
