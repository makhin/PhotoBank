using System;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Enrichers;

public class PreviewEnricher : IEnricher
{
    private readonly IImageService _imageService;
    public EnricherType EnricherType => EnricherType.Preview;
    public Type[] Dependencies => Array.Empty<Type>();

    public PreviewEnricher(IImageService imageService)
    {
        _imageService = imageService;
    }

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
        return Task.CompletedTask;
    }
}
