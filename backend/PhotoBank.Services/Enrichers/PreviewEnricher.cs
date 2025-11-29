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

    public PreviewEnricher(IImageService imageService)
    {
        _imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
    }

    public EnricherType EnricherType => EnricherType.Preview;

    private static readonly Type[] s_dependencies = Array.Empty<Type>();
    public Type[] Dependencies => s_dependencies;

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
        photo.ImageHash = ImageHashHelper.ComputeHash(source.PreviewImage);
        return Task.CompletedTask;
    }
}
