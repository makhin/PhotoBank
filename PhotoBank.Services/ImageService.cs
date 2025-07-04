using ImageMagick;

namespace PhotoBank.Services;

public interface IImageService
{
    void ResizeImage(MagickImage image, out double scale);
}

public class ImageService : IImageService
{
    public const int MaxSize = 1920;

    public void ResizeImage(MagickImage image, out double scale)
    {
        var isLandscape = image.Width > image.Height;
        var maxSize = isLandscape ? image.Width : image.Height;
        scale = 1;

        if (maxSize <= MaxSize) return;

        if (isLandscape)
        {
            scale = ((double)MaxSize / image.Width);
            var geometry = new MagickGeometry(MaxSize, (uint)scale * image.Height);
            image.Resize(geometry);
        }
        else
        {
            scale = ((double)MaxSize / image.Height);
            var geometry = new MagickGeometry((uint)scale * image.Width, MaxSize);
            image.Resize(geometry);
        }
    }
}
