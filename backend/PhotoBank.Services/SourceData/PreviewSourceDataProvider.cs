using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.SourceData;

/// <summary>
/// Provides source data from the preview image stored in S3/MinIO
/// </summary>
public class PreviewSourceDataProvider : ISourceDataProvider
{
    private readonly MinioObjectService _minioObjectService;

    public PreviewSourceDataProvider(MinioObjectService minioObjectService)
    {
        _minioObjectService = minioObjectService;
    }

    public bool CanProvideData(Photo photo, Storage storage)
    {
        return !string.IsNullOrWhiteSpace(photo?.S3Key_Preview);
    }

    public async Task<SourceDataDto> GetSourceDataAsync(Photo photo, Storage storage, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(photo.S3Key_Preview))
        {
            throw new InvalidOperationException($"Preview S3 key is not available for photo {photo.Id}");
        }

        // Download preview from S3/MinIO
        var previewBytes = await _minioObjectService.GetObjectAsync(photo.S3Key_Preview);

        // Create a temporary file for the preview
        var tempPath = Path.Combine(Path.GetTempPath(), $"photobank_preview_{photo.Id}_{Guid.NewGuid()}.jpg");
        await File.WriteAllBytesAsync(tempPath, previewBytes, cancellationToken);

        var sourceData = new SourceDataDto
        {
            AbsolutePath = tempPath
        };

        return sourceData;
    }
}
