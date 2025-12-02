using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Enrichers;

/// <summary>
/// Enricher that checks for duplicate photos across all storages based on ImageHash.
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
        if (string.IsNullOrEmpty(photo.ImageHash))
        {
            return;
        }

        // Search for existing photo with same ImageHash across all storages
        var existingPhoto = await _photoRepository
            .GetByCondition(p => p.ImageHash == photo.ImageHash)
            .AsNoTracking()
            .Include(p => p.Storage)
            .Include(p => p.Files)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingPhoto != null)
        {
            // Mark duplicate information in SourceDataDto
            // PhotoProcessor will use this to add File entry and skip saving new Photo
            sourceData.DuplicatePhotoId = existingPhoto.Id;

            // Get location info from first File for display (cross-storage support)
            var firstFile = existingPhoto.Files?.FirstOrDefault();
            var locationInfo = firstFile != null
                ? $"at {firstFile.RelativePath}"
                : "location unknown";
            sourceData.DuplicatePhotoInfo = $"Photo #{existingPhoto.Id} in storage '{existingPhoto.Storage?.Name}' {locationInfo}";
        }
    }
}
