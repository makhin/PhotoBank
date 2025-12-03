using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PhotoBank.DbContext.DbContext;

namespace PhotoBank.Services;

public interface IPhotoDeduplicationService
{
    Task<int> MergeDuplicatesByImageHashAsync(CancellationToken cancellationToken = default);
}

public class PhotoDeduplicationService : IPhotoDeduplicationService
{
    private readonly PhotoBankDbContext _context;
    private readonly ILogger<PhotoDeduplicationService> _logger;

    public PhotoDeduplicationService(PhotoBankDbContext context, ILogger<PhotoDeduplicationService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> MergeDuplicatesByImageHashAsync(CancellationToken cancellationToken = default)
    {
        var duplicateGroups = await _context.Photos
            .Where(p => p.ImageHash != null)
            .GroupBy(p => p.ImageHash)
            .Select(g => new
            {
                ImageHash = g.Key!,
                PhotoIds = g.Select(p => p.Id).OrderBy(id => id).ToList(),
            })
            .Where(g => g.PhotoIds.Count > 1)
            .ToListAsync(cancellationToken);

        if (duplicateGroups.Count == 0)
        {
            _logger.LogInformation("No duplicate photos found by ImageHash");
            return 0;
        }

        _logger.LogInformation("Found {GroupCount} duplicate groups to merge", duplicateGroups.Count);

        var mergedGroups = 0;

        foreach (var group in duplicateGroups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var photos = await _context.Photos
                    .Where(p => p.ImageHash == group.ImageHash)
                    .Include(p => p.Files)
                    .Include(p => p.Captions)
                    .Include(p => p.PhotoTags)
                    .Include(p => p.PhotoCategories)
                    .Include(p => p.ObjectProperties)
                    .Include(p => p.Faces)
                    .ToListAsync(cancellationToken);

                if (photos.Count <= 1)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    continue;
                }

                var primary = photos.OrderBy(p => p.Id).First();
                var duplicates = photos.Where(p => p.Id != primary.Id).ToList();

                _logger.LogInformation(
                    "Merging {DuplicateCount} duplicate(s) into photo {PrimaryId} for hash {ImageHash}",
                    duplicates.Count,
                    primary.Id,
                    group.ImageHash);

                var existingFileNames = new HashSet<string>(
                    primary.Files.Select(f => f.Name),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var duplicate in duplicates)
                {
                    foreach (var file in duplicate.Files)
                    {
                        if (existingFileNames.Contains(file.Name))
                        {
                            _logger.LogWarning(
                                "Skipping file {FileName} from photo {DuplicateId} because primary photo {PrimaryId} already has a file with the same name",
                                file.Name,
                                duplicate.Id,
                                primary.Id);
                            continue;
                        }

                        file.PhotoId = primary.Id;
                        file.Photo = primary;
                        primary.Files.Add(file);
                        existingFileNames.Add(file.Name);
                    }
                }

                _context.Captions.RemoveRange(duplicates.SelectMany(p => p.Captions));
                _context.PhotoTags.RemoveRange(duplicates.SelectMany(p => p.PhotoTags));
                _context.PhotoCategories.RemoveRange(duplicates.SelectMany(p => p.PhotoCategories));
                _context.ObjectProperties.RemoveRange(duplicates.SelectMany(p => p.ObjectProperties));
                _context.Faces.RemoveRange(duplicates.SelectMany(p => p.Faces));
                _context.Photos.RemoveRange(duplicates);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                mergedGroups++;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to merge duplicates for hash {ImageHash}", group.ImageHash);
                throw;
            }
            finally
            {
                _context.ChangeTracker.Clear();
            }
        }

        _logger.LogInformation("Completed duplicate merge for {MergedGroups} group(s)", mergedGroups);
        return mergedGroups;
    }
}
