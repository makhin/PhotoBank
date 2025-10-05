using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

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

        if (ttlSeconds <= 0)
        {
            _logger.LogWarning(
                "TTL must be positive when resolving media URL. PhotoId: {PhotoId}; FaceId: {FaceId}; Key: {S3Key}.",
                context.PhotoId,
                context.FaceId,
                key);
            return null;
        }

        var s3 = _s3Options.Value ?? new S3Options();

        try
        {
            return await _minioClient.PresignedGetObjectAsync(new PresignedGetObjectArgs()
                .WithBucket(s3.Bucket)
                .WithObject(key)
                .WithExpiry(ttlSeconds)).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to generate presigned URL. PhotoId: {PhotoId}; FaceId: {FaceId}; Key: {S3Key}.",
                context.PhotoId,
                context.FaceId,
                key);
            return null;
        }
    }
}
