using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Internal;

namespace PhotoBank.Services;

public interface IPhotoDeletionService
{
    Task<bool> DeletePhotoAsync(int photoId, CancellationToken cancellationToken = default);
    Task<int> DeleteLastPhotosAsync(int count, CancellationToken cancellationToken = default);
}

public class PhotoDeletionService : IPhotoDeletionService
{
    private readonly PhotoBankDbContext _context;
    private readonly IMinioClient _minioClient;
    private readonly ILogger<PhotoDeletionService> _logger;
    private readonly string _bucket;

    public PhotoDeletionService(
        PhotoBankDbContext context,
        IMinioClient minioClient,
        IOptions<S3Options> s3Options,
        ILogger<PhotoDeletionService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _minioClient = minioClient ?? throw new ArgumentNullException(nameof(minioClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _bucket = (s3Options?.Value ?? new S3Options()).Bucket;
    }

    public async Task<bool> DeletePhotoAsync(int photoId, CancellationToken cancellationToken = default)
    {
        var photo = await LoadPhotoAsync(photoId, cancellationToken);
        if (photo == null)
        {
            _logger.LogInformation("Photo {PhotoId} not found", photoId);
            return false;
        }

        var keysToRemove = CollectS3Objects(photo);
        await RemoveObjectsAsync(keysToRemove, cancellationToken);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        _context.Captions.RemoveRange(photo.Captions);
        _context.PhotoTags.RemoveRange(photo.PhotoTags);
        _context.PhotoCategories.RemoveRange(photo.PhotoCategories);
        _context.ObjectProperties.RemoveRange(photo.ObjectProperties);
        _context.Faces.RemoveRange(photo.Faces);
        _context.Files.RemoveRange(photo.Files);
        _context.Photos.Remove(photo);

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation("Deleted photo {PhotoId} and related data", photoId);
        return true;
    }

    public async Task<int> DeleteLastPhotosAsync(int count, CancellationToken cancellationToken = default)
    {
        if (count <= 0)
        {
            return 0;
        }

        var photoIds = await _context.Photos
            .OrderByDescending(p => p.Id)
            .Select(p => p.Id)
            .Take(count)
            .ToListAsync(cancellationToken);

        var deleted = 0;
        foreach (var photoId in photoIds)
        {
            if (await DeletePhotoAsync(photoId, cancellationToken))
            {
                deleted++;
            }
        }

        return deleted;
    }

    private async Task<Photo?> LoadPhotoAsync(int photoId, CancellationToken cancellationToken)
    {
        return await _context.Photos
            .AsTracking()
            .Include(p => p.Storage)
            .Include(p => p.Files)
            .Include(p => p.Captions)
            .Include(p => p.PhotoTags)
            .Include(p => p.PhotoCategories)
            .Include(p => p.ObjectProperties)
            .Include(p => p.Faces)
            .FirstOrDefaultAsync(p => p.Id == photoId, cancellationToken);
    }

    private IReadOnlyCollection<(string Bucket, string Key)> CollectS3Objects(Photo photo)
    {
        var keys = new HashSet<(string Bucket, string Key)>();

        if (!string.IsNullOrWhiteSpace(photo.S3Key_Preview))
        {
            keys.Add((_bucket, photo.S3Key_Preview));
        }

        if (!string.IsNullOrWhiteSpace(photo.S3Key_Thumbnail))
        {
            keys.Add((_bucket, photo.S3Key_Thumbnail));
        }

        foreach (var faceKey in photo.Faces.Select(f => f.S3Key_Image).Where(k => !string.IsNullOrWhiteSpace(k)))
        {
            keys.Add((_bucket, faceKey!));
        }

        if (TryParseLocation(photo.Storage?.Folder, out var location))
        {
            var prefix = CombinePrefixes(location.Prefix, photo.RelativePath);

            foreach (var fileName in photo.Files.Select(f => f.Name).Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                var key = BuildObjectKey(prefix, fileName!);
                keys.Add((location.Bucket, key));
            }
        }

        return keys;
    }

    private async Task RemoveObjectsAsync(IEnumerable<(string Bucket, string Key)> objects, CancellationToken cancellationToken)
    {
        var failures = new List<Exception>();

        foreach (var (bucket, key) in objects)
        {
            try
            {
                await _minioClient.RemoveObjectAsync(
                    new RemoveObjectArgs()
                        .WithBucket(bucket)
                        .WithObject(key),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove object {Key} from bucket {Bucket}", key, bucket);
                failures.Add(new InvalidOperationException($"Failed to remove object {key} from bucket {bucket}", ex));
            }
        }

        if (failures.Count > 0)
        {
            throw new AggregateException("Failed to remove one or more objects from object storage", failures);
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

        var bucket = string.IsNullOrWhiteSpace(uri.Host) ? _bucket : uri.Host;
        if (string.IsNullOrWhiteSpace(bucket))
        {
            return false;
        }

        var prefix = uri.AbsolutePath.Trim('/');

        location = new ObjectStorageLocation(bucket, string.IsNullOrWhiteSpace(prefix) ? null : prefix);
        return true;
    }

    private static string CombinePrefixes(string? basePrefix, string? relativePath)
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

    private readonly record struct ObjectStorageLocation(string Bucket, string? Prefix);
}
