using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Photos.Upload;

namespace PhotoBank.Services.Photos;

public interface IPhotoIngestionService
{
    Task UploadAsync(IEnumerable<IFormFile> files, int storageId, string? relativePath, CancellationToken cancellationToken = default);
}

public class PhotoIngestionService : IPhotoIngestionService
{
    private readonly IRepository<Storage> _storageRepository;
    private readonly IEnumerable<IStorageUploadStrategy> _uploadStrategies;
    private readonly ILogger<PhotoIngestionService> _logger;

    public PhotoIngestionService(
        IRepository<Storage> storageRepository,
        IEnumerable<IStorageUploadStrategy> uploadStrategies,
        ILogger<PhotoIngestionService> logger)
    {
        _storageRepository = storageRepository;
        _uploadStrategies = uploadStrategies;
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

        var strategy = _uploadStrategies.FirstOrDefault(s => s.CanHandle(storage));
        if (strategy == null)
        {
            throw new InvalidOperationException($"No upload strategy registered for storage {storage.Id}.");
        }

        await strategy.UploadAsync(storage, files, relativePath, cancellationToken);
    }
}
