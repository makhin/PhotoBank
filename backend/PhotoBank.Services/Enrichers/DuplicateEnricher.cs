using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Models;
using File = PhotoBank.DbContext.Models.File;

namespace PhotoBank.Services.Enrichers;

/// <summary>
/// Enricher that prepares photo for duplicate detection and checks for duplicates.
/// Computes ImageHash, creates initial File entry, then searches for duplicates across all storages.
/// If a duplicate is found, marks it in SourceDataDto for PhotoProcessor to handle.
/// This enricher does not write to the database - it only detects duplicates.
/// </summary>
public sealed class DuplicateEnricher : IEnricher
{
    private readonly IRepository<Photo> _photoRepository;

    public DuplicateEnricher(IRepository<Photo> photoRepository)
    {
        _photoRepository = photoRepository ?? throw new ArgumentNullException(nameof(photoRepository));
    }

    public EnricherType EnricherType => EnricherType.Duplicate;

    public Type[] Dependencies => [typeof(PreviewEnricher)];

    public async Task EnrichAsync(Photo photo, SourceDataDto sourceData, CancellationToken cancellationToken = default)
    {
        // Compute ImageHash from preview image (moved from PreviewEnricher)
        if (sourceData.PreviewImage != null)
        {
            photo.ImageHash = ImageHashHelper.ComputeHash(sourceData.PreviewImage);
        }

        // Extract Name and RelativePath from file path (moved from MetadataEnricher)
        var normalizedAbsolutePath = sourceData.AbsolutePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        var normalizedStoragePath = sourceData.Storage.Folder.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

        photo.Name = Path.GetFileNameWithoutExtension(normalizedAbsolutePath);
        var relativePath = Path.GetDirectoryName(normalizedAbsolutePath)?
            .Replace(normalizedStoragePath, string.Empty)
            .TrimStart(Path.DirectorySeparatorChar);

        // Create initial File entry (moved from MetadataEnricher)
        photo.Files = new List<File>
        {
            new()
            {
                StorageId = sourceData.Storage.Id,
                RelativePath = relativePath,
                Name = Path.GetFileName(normalizedAbsolutePath)
            }
        };

        if (string.IsNullOrEmpty(photo.ImageHash))
        {
            return;
        }

        // Search for existing photo with same ImageHash across all storages
        var existingPhoto = await _photoRepository
            .GetByCondition(p => p.ImageHash == photo.ImageHash && p.Id != photo.Id)
            .AsNoTracking()
            .Include(p => p.Files)
                .ThenInclude(f => f.Storage)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingPhoto != null)
        {
            // Mark duplicate information in SourceDataDto
            // PhotoProcessor will use this to add File entry and skip saving new Photo
            sourceData.DuplicatePhotoId = existingPhoto.Id;

            // Get location info from first File for display (cross-storage support)
            var firstFile = existingPhoto.Files?.FirstOrDefault();
            var locationInfo = firstFile != null
                ? $"at {firstFile.RelativePath} in storage '{firstFile.Storage?.Name}'"
                : "location unknown";
            sourceData.DuplicatePhotoInfo = $"Photo #{existingPhoto.Id} {locationInfo}";
        }
    }
}
