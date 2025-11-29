using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Models;
using Smartcrop;

namespace PhotoBank.Services.Enrichers;

public sealed class ThumbnailEnricher : IEnricher
{
    public EnricherType EnricherType => EnricherType.Thumbnail;

    private static readonly Type[] s_dependencies = { typeof(PreviewEnricher) };
    public Type[] Dependencies => s_dependencies;

    private const int Width = 50;
    private const int Height = 50;

    public Task EnrichAsync(Photo photo, SourceDataDto source, CancellationToken cancellationToken = default)
    {
        if (source?.PreviewImage is null)
            return Task.CompletedTask;

        // Convert IMagickImage to stream for smartcrop
        using var imageStream = new MemoryStream(source.PreviewImageBytes);

        // Find optimal crop area using smartcrop.net
        var cropResult = new ImageCrop(Width, Height).Crop(imageStream);

        // Apply crop and resize using Magick.NET
        using var magickImage = source.PreviewImage.Clone();

        // Crop to the area found by smartcrop
        var cropGeometry = new MagickGeometry(
            (int)cropResult.Area.X,
            (int)cropResult.Area.Y,
            (uint)cropResult.Area.Width,
            (uint)cropResult.Area.Height);

        magickImage.Crop(cropGeometry);

        // Resize to final thumbnail size
        magickImage.Resize(Width, Height);
        magickImage.Format = MagickFormat.Jpg;

        // Convert to byte array
        source.ThumbnailImage = magickImage.ToByteArray();

        return Task.CompletedTask;
    }
}

