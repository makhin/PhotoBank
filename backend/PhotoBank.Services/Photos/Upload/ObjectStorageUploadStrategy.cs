using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Internal;

namespace PhotoBank.Services.Photos.Upload;

public sealed class ObjectStorageUploadStrategy : IStorageUploadStrategy
{
    private readonly IMinioClient _minioClient;
    private readonly S3Options _options;
    private readonly UploadNameResolver _nameResolver;
    private readonly ILogger<ObjectStorageUploadStrategy> _logger;

    public ObjectStorageUploadStrategy(
        IMinioClient minioClient,
        IOptions<S3Options> options,
        UploadNameResolver nameResolver,
        ILogger<ObjectStorageUploadStrategy> logger)
    {
        _minioClient = minioClient;
        _options = options?.Value ?? new S3Options();
        _nameResolver = nameResolver;
        _logger = logger;
    }

    public bool CanHandle(Storage storage)
    {
        if (storage == null)
        {
            return false;
        }

        return TryParseLocation(storage.Folder, out _);
    }

    public async Task UploadAsync(
        Storage storage,
        IEnumerable<IFormFile> files,
        string? relativePath,
        CancellationToken cancellationToken)
    {
        if (!TryParseLocation(storage.Folder, out var location))
        {
            throw new InvalidOperationException($"Storage {storage.Id} is not configured for object storage uploads.");
        }

        var prefix = CombinePrefixes(location.Prefix, relativePath);

        foreach (var file in files)
        {
            var resolution = await _nameResolver.ResolveAsync(
                file.FileName,
                file.Length,
                candidate => TryStatAsync(location.Bucket, BuildObjectKey(prefix, candidate), cancellationToken),
                cancellationToken).ConfigureAwait(false);

            if (!resolution.ShouldUpload)
            {
                _logger.LogInformation(
                    "Skipping upload for {FileName} - identical object already exists in bucket {Bucket}",
                    file.FileName,
                    location.Bucket);
                continue;
            }

            var key = BuildObjectKey(prefix, resolution.TargetName);

            await using var stream = file.OpenReadStream();
            await _minioClient.PutObjectAsync(new PutObjectArgs()
                    .WithBucket(location.Bucket)
                    .WithObject(key)
                    .WithStreamData(stream)
                    .WithObjectSize(file.Length)
                    .WithContentType(string.IsNullOrWhiteSpace(file.ContentType)
                        ? "application/octet-stream"
                        : file.ContentType),
                cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Uploaded {FileName} to bucket {Bucket} with key {ObjectKey} for storage {StorageId}",
                file.FileName,
                location.Bucket,
                key,
                storage.Id);
        }
    }

    private async Task<UploadNameResolver.StoredObjectInfo?> TryStatAsync(
        string bucket,
        string objectKey,
        CancellationToken cancellationToken)
    {
        try
        {
            var stat = await _minioClient.StatObjectAsync(new StatObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(objectKey),
                cancellationToken).ConfigureAwait(false);

            return new UploadNameResolver.StoredObjectInfo(stat.Size);
        }
        catch (Exception ex) when (IsNotFound(ex))
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to inspect object {ObjectKey} in bucket {Bucket}", objectKey, bucket);
            return null;
        }
    }

    private bool TryParseLocation(string? folder, out ObjectStorageLocation location)
    {
        location = default;

        if (string.IsNullOrWhiteSpace(folder))
        {
            return false;
        }

        if (!Uri.TryCreate(folder, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!string.Equals(uri.Scheme, "s3", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, "minio", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var bucket = string.IsNullOrWhiteSpace(uri.Host) ? _options.Bucket : uri.Host;
        if (string.IsNullOrWhiteSpace(bucket))
        {
            bucket = _options.Bucket;
        }
        var prefix = uri.AbsolutePath.Trim('/');

        location = new ObjectStorageLocation(bucket, string.IsNullOrWhiteSpace(prefix) ? null : prefix);
        return true;
    }

    private bool IsNotFound(Exception ex)
    {
        if (ex.GetType().Name.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase);
    }

    private string CombinePrefixes(string? basePrefix, string? relativePath)
    {
        var segments = new List<string>();
        if (!string.IsNullOrWhiteSpace(basePrefix))
        {
            segments.Add(basePrefix.Trim('/'));
        }

        if (!string.IsNullOrWhiteSpace(relativePath))
        {
            segments.Add(relativePath.Replace('\\', '/').Trim('/'));
        }

        return string.Join('/', segments.Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    private static string BuildObjectKey(string prefix, string fileName)
    {
        var normalized = fileName.Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return normalized;
        }

        return string.Join('/', new[] { prefix.TrimEnd('/'), normalized.TrimStart('/') });
    }

    private readonly struct ObjectStorageLocation
    {
        public ObjectStorageLocation(string bucket, string? prefix)
        {
            Bucket = bucket;
            Prefix = prefix;
        }

        public string Bucket { get; }

        public string? Prefix { get; }
    }
}
