using System;
using System.Collections.Generic;
using System.IO;
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
using PhotoBank.Repositories;
using PhotoBank.Services.Internal;
using System.IO.Abstractions;

namespace PhotoBank.Services.Photos;

public interface IPhotoIngestionService
{
    Task UploadAsync(IEnumerable<IFormFile> files, int storageId, string? relativePath, CancellationToken cancellationToken = default);
}

public class PhotoIngestionService : IPhotoIngestionService
{
    private readonly IRepository<Storage> _storageRepository;
    private readonly IMinioClient _minioClient;
    private readonly S3Options _s3Options;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<PhotoIngestionService> _logger;

    public PhotoIngestionService(
        IRepository<Storage> storageRepository,
        IMinioClient minioClient,
        IOptions<S3Options> s3Options,
        IFileSystem fileSystem,
        ILogger<PhotoIngestionService> logger)
    {
        _storageRepository = storageRepository;
        _minioClient = minioClient;
        _s3Options = s3Options?.Value ?? new S3Options();
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public async Task UploadAsync(IEnumerable<IFormFile> files, int storageId, string? relativePath, CancellationToken cancellationToken = default)
    {
        if (files == null || !files.Any())
        {
            _logger.LogDebug("No files provided for upload to storage {StorageId}", storageId);
            return;
        }

        var storage = await _storageRepository.GetAsync(storageId);
        if (storage == null)
        {
            throw new ArgumentException($"Storage {storageId} not found", nameof(storageId));
        }

        if (IsObjectStorageLocation(storage.Folder, out var bucket, out var prefix))
        {
            var resolvedBucket = string.IsNullOrWhiteSpace(bucket) ? _s3Options.Bucket : bucket;
            await UploadToObjectStorageAsync(storageId, resolvedBucket, prefix, files, relativePath, cancellationToken);
            return;
        }

        await UploadToFileSystemAsync(storageId, storage.Folder, files, relativePath, cancellationToken);
    }

    private async Task UploadToFileSystemAsync(
        int storageId,
        string? root,
        IEnumerable<IFormFile> files,
        string? relativePath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new InvalidOperationException($"Storage {storageId} does not have a folder configured.");
        }

        var targetPath = string.IsNullOrEmpty(relativePath)
            ? root
            : _fileSystem.Path.Combine(root, relativePath);

        if (!_fileSystem.Directory.Exists(targetPath))
        {
            _fileSystem.Directory.CreateDirectory(targetPath);
        }

        foreach (var file in files)
        {
            var destination = _fileSystem.Path.Combine(targetPath, file.FileName);

            if (_fileSystem.File.Exists(destination))
            {
                var existing = _fileSystem.FileInfo.New(destination);
                if (existing.Length == file.Length)
                {
                    _logger.LogInformation(
                        "Skipping upload for {FileName} - identical file already exists in storage {StorageId}",
                        file.FileName,
                        storageId);
                    continue;
                }

                var baseName = _fileSystem.Path.GetFileNameWithoutExtension(file.FileName);
                var extension = _fileSystem.Path.GetExtension(file.FileName);
                var index = 1;
                string candidate;
                do
                {
                    candidate = _fileSystem.Path.Combine(targetPath, $"{baseName}_{index}{extension}");
                    index++;
                } while (_fileSystem.File.Exists(candidate));

                destination = candidate;
            }

            await using var stream = _fileSystem.File.Create(destination);
            await file.CopyToAsync(stream, cancellationToken);
        }
    }

    private async Task UploadToObjectStorageAsync(
        int storageId,
        string bucket,
        string? basePrefix,
        IEnumerable<IFormFile> files,
        string? relativePath,
        CancellationToken cancellationToken)
    {
        var prefix = CombinePrefixes(basePrefix, relativePath);

        foreach (var file in files)
        {
            var key = BuildObjectKey(prefix, file.FileName);
            var candidateKey = key;
            var index = 1;

            while (true)
            {
                var stat = await TryStatObjectAsync(bucket, candidateKey, cancellationToken);
                if (stat == null)
                {
                    break;
                }

                if (stat.Size == file.Length)
                {
                    _logger.LogInformation(
                        "Skipping upload for {FileName} - identical object already exists in bucket {Bucket}",
                        file.FileName,
                        bucket);
                    goto ContinueWithNextFile;
                }

                var baseName = Path.GetFileNameWithoutExtension(file.FileName);
                var extension = Path.GetExtension(file.FileName);
                candidateKey = BuildObjectKey(prefix, $"{baseName}_{index}{extension}");
                index++;
            }

            await using (var stream = file.OpenReadStream())
            {
                await _minioClient.PutObjectAsync(new PutObjectArgs()
                        .WithBucket(bucket)
                        .WithObject(candidateKey)
                        .WithStreamData(stream)
                        .WithObjectSize(file.Length)
                        .WithContentType(string.IsNullOrWhiteSpace(file.ContentType)
                            ? "application/octet-stream"
                            : file.ContentType),
                    cancellationToken);
            }

            _logger.LogInformation(
                "Uploaded {FileName} to bucket {Bucket} with key {ObjectKey} for storage {StorageId}",
                file.FileName,
                bucket,
                candidateKey,
                storageId);

        ContinueWithNextFile: ;
        }
    }

    private async Task<ObjectStat?> TryStatObjectAsync(string bucket, string objectKey, CancellationToken cancellationToken)
    {
        try
        {
            return await _minioClient.StatObjectAsync(new StatObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectKey), cancellationToken);
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

    private bool IsObjectStorageLocation(string? folder, out string bucket, out string? prefix)
    {
        bucket = string.Empty;
        prefix = null;
        if (string.IsNullOrWhiteSpace(folder))
        {
            bucket = _s3Options.Bucket;
            return false;
        }

        if (Uri.TryCreate(folder, UriKind.Absolute, out var uri) &&
            (string.Equals(uri.Scheme, "s3", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(uri.Scheme, "minio", StringComparison.OrdinalIgnoreCase)))
        {
            bucket = string.IsNullOrWhiteSpace(uri.Host) ? string.Empty : uri.Host;
            prefix = uri.AbsolutePath.Trim('/');
            return true;
        }

        return false;
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

        var prefix = string.Join('/', segments.Where(s => !string.IsNullOrWhiteSpace(s)));
        return prefix;
    }

    private string BuildObjectKey(string prefix, string fileName)
    {
        var normalized = fileName.Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return normalized;
        }

        return string.Join('/', new[] { prefix.TrimEnd('/'), normalized.TrimStart('/') });
    }
}
