using System;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Enrichers;

public sealed class PreviewEnricher : IEnricher
{
    private readonly IImageService _imageService;
    private const int LetterboxSize = 640;

    public PreviewEnricher(IImageService imageService)
    {
        _imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
    }

    public EnricherType EnricherType => EnricherType.Preview;

    public Type[] Dependencies => [];

    public Task EnrichAsync(Photo photo, SourceDataDto source, CancellationToken cancellationToken = default)
    {
        using var image = new MagickImage(source.AbsolutePath);
        image.AutoOrient();
        source.OriginalImage = image.Clone();
        photo.Height = image.Height;
        photo.Width = image.Width;
        photo.Orientation = (int?)image.Orientation;
        _imageService.ResizeImage(image, out var scale);
        image.Format = MagickFormat.Jpg;
        photo.Scale = scale;
        source.PreviewImage = image.Clone();
        // ImageHash computation moved to DuplicateEnricher

        // Create letterboxed 640x640 image for ONNX models (YOLO, NudeNet)
        CreateLetterboxedImage(source.OriginalImage, source);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a 640x640 letterboxed image from the original image.
    /// Preserves aspect ratio and adds black padding.
    /// </summary>
    private static void CreateLetterboxedImage(IMagickImage<byte> originalImage, SourceDataDto source)
    {
        var originalWidth = (int)originalImage.Width;
        var originalHeight = (int)originalImage.Height;

        // Calculate scale to fit image into 640x640 while preserving aspect ratio
        var scale = Math.Min((float)LetterboxSize / originalWidth, (float)LetterboxSize / originalHeight);

        // Calculate new dimensions after scaling
        var newWidth = (uint)(originalWidth * scale);
        var newHeight = (uint)(originalHeight * scale);

        // Calculate padding to center the image in 640x640
        var padX = (LetterboxSize - (int)newWidth) / 2;
        var padY = (LetterboxSize - (int)newHeight) / 2;

        // Create letterboxed image (640x640 with black padding)
        var letterboxed = new MagickImage(MagickColors.Black, (uint)LetterboxSize, (uint)LetterboxSize);

        // Resize original image preserving aspect ratio
        using var resized = originalImage.Clone();
        resized.Resize(newWidth, newHeight);

        // Copy resized image to center of letterboxed canvas
        letterboxed.Composite(resized, padX, padY, CompositeOperator.Over);

        // Ensure RGB format
        letterboxed.ColorSpace = ColorSpace.sRGB;

        // Store letterboxed image and parameters
        source.LetterboxedImage640 = letterboxed;
        source.LetterboxScale = scale;
        source.LetterboxPadX = padX;
        source.LetterboxPadY = padY;
    }
}
