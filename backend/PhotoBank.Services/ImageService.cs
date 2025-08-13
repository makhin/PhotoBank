using System;
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

        if (maxSize > MaxSize)
        {
            if (isLandscape)
            {
                scale = MaxSize / (double)image.Width;
                var newW = (uint)MaxSize;
                var newH = (uint)Math.Max(1, (int)(scale * image.Height));
                image.Resize(new MagickGeometry(newW, newH) { IgnoreAspectRatio = false });
            }
            else
            {
                scale = MaxSize / (double)image.Height;
                var newH = (uint)MaxSize;
                var newW = (uint)Math.Max(1, (int)(scale * image.Width));
                image.Resize(new MagickGeometry(newW, newH) { IgnoreAspectRatio = false });
            }
        }
        else
        {
            scale = 1;
        }

        // Сжимаем вес превью
        image.Strip();                 // убрать метаданные
        image.Quality = 82;            // компромисс качество/размер
        image.Settings.Interlace = Interlace.Jpeg; // прогрессивный JPEG
        image.Format = MagickFormat.Jpg;
    }
}
