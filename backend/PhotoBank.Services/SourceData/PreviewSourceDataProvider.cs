using System;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.SourceData;

/// <summary>
/// Provides source data from the preview image stored in S3/MinIO.
/// Populates PreviewImage and OriginalImage for enrichers that work with image data
/// (Face, Analyze, Tag, etc). Does not provide AbsolutePath - for file-based enrichers
/// like MetadataEnricher, use OriginalFileSourceDataProvider instead.
/// </summary>
public class PreviewSourceDataProvider : ISourceDataProvider
{
    private readonly MinioObjectService _minioObjectService;

    public PreviewSourceDataProvider(MinioObjectService minioObjectService)
    {
        _minioObjectService = minioObjectService ?? throw new ArgumentNullException(nameof(minioObjectService));
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

        // Load the preview image into MagickImage for enrichers that need PreviewImage
        // Note: Since we're loading from preview (not original), both Original and Preview
        // will point to the same image data
        var previewImage = new MagickImage(previewBytes);

        var sourceData = new SourceDataDto
        {
            // AbsolutePath not meaningful for preview provider - enrichers using preview
            // (Face, Analyze, Tag, etc) work with PreviewImage/ImageAnalysis, not file paths.
            // MetadataEnricher (which needs AbsolutePath) should use OriginalFileSourceDataProvider.
            AbsolutePath = string.Empty,
            PreviewImage = previewImage,
            OriginalImage = previewImage.Clone() // Clone to avoid disposal issues
        };

        return sourceData;
    }
}
