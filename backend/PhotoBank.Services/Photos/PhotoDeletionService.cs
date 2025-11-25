using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;

namespace PhotoBank.Services.Photos;

public interface IPhotoDeletionService
{
    Task<PhotoDeletionResult> DeletePhotoAsync(int photoId, CancellationToken ct = default);
}

public class PhotoDeletionService : IPhotoDeletionService
{
    private readonly PhotoBankDbContext _context;
    private readonly IRepository<Photo> _photoRepository;
    private readonly MinioObjectService _objectService;
    private readonly ILogger<PhotoDeletionService> _logger;

    public PhotoDeletionService(
        PhotoBankDbContext context,
        IRepository<Photo> photoRepository,
        MinioObjectService objectService,
        ILogger<PhotoDeletionService> logger)
    {
        _context = context;
        _photoRepository = photoRepository;
        _objectService = objectService;
        _logger = logger;
    }

    public async Task<PhotoDeletionResult> DeletePhotoAsync(int photoId, CancellationToken ct = default)
    {
        var result = new PhotoDeletionResult { PhotoId = photoId };

        try
        {
            // Get photo with S3 keys before deletion
            var photo = await _photoRepository.GetAll()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == photoId, ct);

            if (photo == null)
            {
                result.Success = false;
                result.ErrorMessage = $"Photo with ID {photoId} not found";
                _logger.LogWarning("Photo {PhotoId} not found", photoId);
                return result;
            }

            _logger.LogInformation("Deleting photo {PhotoId}", photoId);

            // Delete S3 objects if they exist
            await DeleteS3ObjectsAsync(photo, result, ct);

            // Delete database records in correct order to avoid FK violations
            await DeleteDatabaseRecordsAsync(photoId, result, ct);

            result.Success = true;
            _logger.LogInformation("Successfully deleted photo {PhotoId}", photoId);
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Error deleting photo: {ex.Message}";
            _logger.LogError(ex, "Error deleting photo {PhotoId}", photoId);
            return result;
        }
    }

    private async Task DeleteS3ObjectsAsync(Photo photo, PhotoDeletionResult result, CancellationToken ct)
    {
        // Delete preview image
        if (!string.IsNullOrEmpty(photo.S3Key_Preview))
        {
            try
            {
                var exists = await _objectService.ObjectExistsAsync(photo.S3Key_Preview);
                if (exists)
                {
                    await _objectService.DeleteObjectAsync(photo.S3Key_Preview);
                    result.DeletedPreview = true;
                    _logger.LogDebug("Deleted preview: {S3Key}", photo.S3Key_Preview);
                }
                else
                {
                    _logger.LogDebug("Preview not found in S3: {S3Key}", photo.S3Key_Preview);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete preview {S3Key}", photo.S3Key_Preview);
                result.PreviewDeletionError = ex.Message;
            }
        }

        // Delete thumbnail image
        if (!string.IsNullOrEmpty(photo.S3Key_Thumbnail))
        {
            try
            {
                var exists = await _objectService.ObjectExistsAsync(photo.S3Key_Thumbnail);
                if (exists)
                {
                    await _objectService.DeleteObjectAsync(photo.S3Key_Thumbnail);
                    result.DeletedThumbnail = true;
                    _logger.LogDebug("Deleted thumbnail: {S3Key}", photo.S3Key_Thumbnail);
                }
                else
                {
                    _logger.LogDebug("Thumbnail not found in S3: {S3Key}", photo.S3Key_Thumbnail);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete thumbnail {S3Key}", photo.S3Key_Thumbnail);
                result.ThumbnailDeletionError = ex.Message;
            }
        }
    }

    private async Task DeleteDatabaseRecordsAsync(int photoId, PhotoDeletionResult result, CancellationToken ct)
    {
        // Use raw SQL to delete records in correct order
        // This is more efficient than loading entities and ensures proper FK order

        // Delete Captions (must be first due to FK constraint)
        var deletedCaptions = await _context.Database.ExecuteSqlRawAsync(
            @"DELETE FROM ""Captions"" WHERE ""PhotoId"" = {0}", photoId);
        result.DeletedCaptions = deletedCaptions;

        // Delete PhotoTags
        var deletedPhotoTags = await _context.Database.ExecuteSqlRawAsync(
            @"DELETE FROM ""PhotoTags"" WHERE ""PhotoId"" = {0}", photoId);
        result.DeletedPhotoTags = deletedPhotoTags;

        // Delete PhotoCategories
        var deletedPhotoCategories = await _context.Database.ExecuteSqlRawAsync(
            @"DELETE FROM ""PhotoCategories"" WHERE ""PhotoId"" = {0}", photoId);
        result.DeletedPhotoCategories = deletedPhotoCategories;

        // Delete ObjectProperties
        var deletedObjectProperties = await _context.Database.ExecuteSqlRawAsync(
            @"DELETE FROM ""ObjectProperties"" WHERE ""PhotoId"" = {0}", photoId);
        result.DeletedObjectProperties = deletedObjectProperties;

        // Delete Faces
        var deletedFaces = await _context.Database.ExecuteSqlRawAsync(
            @"DELETE FROM ""Faces"" WHERE ""PhotoId"" = {0}", photoId);
        result.DeletedFaces = deletedFaces;

        // Delete Files
        var deletedFiles = await _context.Database.ExecuteSqlRawAsync(
            @"DELETE FROM ""Files"" WHERE ""PhotoId"" = {0}", photoId);
        result.DeletedFiles = deletedFiles;

        // Finally, delete the Photo itself
        var deletedPhoto = await _context.Database.ExecuteSqlRawAsync(
            @"DELETE FROM ""Photos"" WHERE ""Id"" = {0}", photoId);
        result.DeletedPhoto = deletedPhoto > 0;

        _logger.LogDebug(
            "Deleted database records for photo {PhotoId}: Captions={Captions}, Tags={Tags}, Categories={Categories}, Properties={Properties}, Faces={Faces}, Files={Files}",
            photoId, deletedCaptions, deletedPhotoTags, deletedPhotoCategories, deletedObjectProperties, deletedFaces, deletedFiles);
    }
}

public class PhotoDeletionResult
{
    public int PhotoId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    // S3 deletion results
    public bool DeletedPreview { get; set; }
    public bool DeletedThumbnail { get; set; }
    public string? PreviewDeletionError { get; set; }
    public string? ThumbnailDeletionError { get; set; }

    // Database deletion counts
    public int DeletedCaptions { get; set; }
    public int DeletedPhotoTags { get; set; }
    public int DeletedPhotoCategories { get; set; }
    public int DeletedObjectProperties { get; set; }
    public int DeletedFaces { get; set; }
    public int DeletedFiles { get; set; }
    public bool DeletedPhoto { get; set; }

    public void PrintSummary()
    {
        Console.WriteLine($"Photo ID: {PhotoId}");
        Console.WriteLine($"Status: {(Success ? "SUCCESS" : "FAILED")}");

        if (!Success)
        {
            Console.WriteLine($"Error: {ErrorMessage}");
            return;
        }

        Console.WriteLine("\nS3 Objects:");
        Console.WriteLine($"  Preview: {(DeletedPreview ? "Deleted" : "Not found/error")}");
        if (!string.IsNullOrEmpty(PreviewDeletionError))
            Console.WriteLine($"    Error: {PreviewDeletionError}");

        Console.WriteLine($"  Thumbnail: {(DeletedThumbnail ? "Deleted" : "Not found/error")}");
        if (!string.IsNullOrEmpty(ThumbnailDeletionError))
            Console.WriteLine($"    Error: {ThumbnailDeletionError}");

        Console.WriteLine("\nDatabase Records:");
        Console.WriteLine($"  Captions: {DeletedCaptions}");
        Console.WriteLine($"  PhotoTags: {DeletedPhotoTags}");
        Console.WriteLine($"  PhotoCategories: {DeletedPhotoCategories}");
        Console.WriteLine($"  ObjectProperties: {DeletedObjectProperties}");
        Console.WriteLine($"  Faces: {DeletedFaces}");
        Console.WriteLine($"  Files: {DeletedFiles}");
        Console.WriteLine($"  Photo: {(DeletedPhoto ? "Deleted" : "Not deleted")}");
    }
}
