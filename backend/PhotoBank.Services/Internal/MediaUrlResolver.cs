using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;

namespace PhotoBank.Services.Internal;

public readonly record struct MediaUrlContext(int? PhotoId, int? FaceId)
{
    public static MediaUrlContext ForPhoto(int? photoId) => new(photoId, null);

    public static MediaUrlContext ForFace(int? photoId, int? faceId) => new(photoId, faceId);
}

public interface IMediaUrlResolver
{
    Task<string?> ResolveAsync(string? key, int ttlSeconds, MediaUrlContext context, CancellationToken cancellationToken = default);
}

public sealed class MediaUrlResolver : IMediaUrlResolver
{
    private readonly IMinioClient _minioClient;
    private readonly IOptions<S3Options> _s3Options;
    private readonly ILogger<MediaUrlResolver> _logger;

    public MediaUrlResolver(
        IMinioClient minioClient,
        IOptions<S3Options> s3Options,
        ILogger<MediaUrlResolver> logger)
    {
        _minioClient = minioClient ?? throw new ArgumentNullException(nameof(minioClient));
        _s3Options = s3Options ?? throw new ArgumentNullException(nameof(s3Options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string?> ResolveAsync(string? key, int ttlSeconds, MediaUrlContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var s3 = _s3Options.Value ?? new S3Options();

        // Возвращаем относительный путь через nginx proxy
        // Это позволяет URL автоматически работать с любым хостом (makhin.ddns.net или raspberrypi.local)
        var relativePath = $"/minio/{s3.Bucket}/{key}";

        _logger.LogDebug(
            "Resolved media URL to relative path. PhotoId: {PhotoId}; FaceId: {FaceId}; Key: {S3Key}; Path: {Path}.",
            context.PhotoId,
            context.FaceId,
            key,
            relativePath);

        return await Task.FromResult(relativePath);
    }
}
