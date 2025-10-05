using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;

namespace PhotoBank.Services.Photos.Admin;

public interface IPhotoAdminService
{
    Task UploadPhotosAsync(IEnumerable<IFormFile> files, int storageId, string path);
}

public class PhotoAdminService : IPhotoAdminService
{
    private readonly IRepository<Storage> _storageRepository;
    private readonly ILogger<PhotoAdminService> _logger;

    public PhotoAdminService(IRepository<Storage> storageRepository, ILogger<PhotoAdminService> logger)
    {
        _storageRepository = storageRepository;
        _logger = logger;
    }

    public async Task UploadPhotosAsync(IEnumerable<IFormFile> files, int storageId, string path)
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

        var targetPath = Path.Combine(storage.Folder, path ?? string.Empty);

        if (!Directory.Exists(targetPath))
        {
            Directory.CreateDirectory(targetPath);
        }

        foreach (var file in files)
        {
            var destination = Path.Combine(targetPath, file.FileName);

            if (System.IO.File.Exists(destination))
            {
                var existing = new FileInfo(destination);
                if (existing.Length == file.Length)
                {
                    _logger.LogInformation("Skipping upload for {FileName} - identical file already exists in storage {StorageId}", file.FileName, storageId);
                    continue;
                }

                var name = Path.GetFileNameWithoutExtension(file.FileName);
                var extension = Path.GetExtension(file.FileName);
                var index = 1;
                do
                {
                    var newFileName = $"{name}_{index}{extension}";
                    destination = Path.Combine(targetPath, newFileName);
                    index++;
                } while (System.IO.File.Exists(destination));
            }

            await using var stream = new FileStream(destination, FileMode.Create);
            await file.CopyToAsync(stream);
        }
    }
}
