using System;
using System.Drawing.Imaging;
using ImageMagick;

namespace PhotoBank.Services
{
    public class InternalMagickFormatInfo
    {
        internal static MagickFormat GetFormat(ImageFormat format)
        {
            if (format == ImageFormat.Bmp || format == ImageFormat.MemoryBmp)
                return MagickFormat.Bmp;
            else if (format == ImageFormat.Gif)
                return MagickFormat.Gif;
            else if (format == ImageFormat.Icon)
                return MagickFormat.Icon;
            else if (format == ImageFormat.Jpeg)
                return MagickFormat.Jpeg;
            else if (format == ImageFormat.Png)
                return MagickFormat.Png;
            else if (format == ImageFormat.Tiff)
                return MagickFormat.Tiff;
            else
                throw new NotSupportedException("Unsupported image format: " + format.ToString());
        }
    }
}