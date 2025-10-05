using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PhotoBank.DbContext.Models;

namespace PhotoBank.Services.Photos.Upload;

public sealed class FileSystemStorageUploadStrategy : IStorageUploadStrategy
{
    private readonly IFileSystem _fileSystem;
    private readonly UploadNameResolver _nameResolver;
    private readonly ILogger<FileSystemStorageUploadStrategy> _logger;

    public FileSystemStorageUploadStrategy(
        IFileSystem fileSystem,
        UploadNameResolver nameResolver,
        ILogger<FileSystemStorageUploadStrategy> logger)
    {
        _fileSystem = fileSystem;
        _nameResolver = nameResolver;
        _logger = logger;
    }

    public bool CanHandle(Storage storage)
    {
        if (storage == null)
        {
            return false;
        }

        var folder = storage.Folder;
        if (string.IsNullOrWhiteSpace(folder))
        {
            return true;
        }

        return !IsObjectStorageUri(folder);
    }

    public async Task UploadAsync(
        Storage storage,
        IEnumerable<IFormFile> files,
        string? relativePath,
        CancellationToken cancellationToken)
    {
        var root = storage.Folder;
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new InvalidOperationException($"Storage {storage.Id} does not have a folder configured.");
        }

        var targetPath = string.IsNullOrWhiteSpace(relativePath)
            ? root
            : _fileSystem.Path.Combine(root, relativePath);

        if (!_fileSystem.Directory.Exists(targetPath))
        {
            _fileSystem.Directory.CreateDirectory(targetPath);
        }

        foreach (var file in files)
        {
            var resolution = await _nameResolver.ResolveAsync(
                file.FileName,
                file.Length,
                candidate => Task.FromResult<UploadNameResolver.StoredObjectInfo?>(GetExistingFile(targetPath, candidate)),
                cancellationToken).ConfigureAwait(false);

            if (!resolution.ShouldUpload)
            {
                _logger.LogInformation(
                    "Skipping upload for {FileName} - identical file already exists in storage {StorageId}",
                    file.FileName,
                    storage.Id);
                continue;
            }

            var destination = _fileSystem.Path.Combine(targetPath, resolution.TargetName);
            await using var stream = _fileSystem.File.Create(destination);
            await file.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
        }
    }

    private UploadNameResolver.StoredObjectInfo? GetExistingFile(string targetPath, string candidate)
    {
        var destination = _fileSystem.Path.Combine(targetPath, candidate);
        if (!_fileSystem.File.Exists(destination))
        {
            return null;
        }

        var existing = _fileSystem.FileInfo.New(destination);
        return new UploadNameResolver.StoredObjectInfo(existing.Length);
    }

    private static bool IsObjectStorageUri(string folder)
    {
        if (!Uri.TryCreate(folder, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return string.Equals(uri.Scheme, "s3", StringComparison.OrdinalIgnoreCase)
               || string.Equals(uri.Scheme, "minio", StringComparison.OrdinalIgnoreCase);
    }
}
