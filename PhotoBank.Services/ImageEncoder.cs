using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using ImageMagick;

namespace PhotoBank.Services
{
    public interface IImageEncoder
    {
        byte[] Prepare(string path, out double scale);
    }

    public class ImageEncoder : IImageEncoder
    {
        public const int MaxSize = 1920;

        public byte[] Prepare(string path, out double scale)
        {
            var stream = new MemoryStream();
            scale = 1;
            using (var image = new MagickImage(path))
            {
                var isLandscape = image.Width > image.Height;
                var maxSize = isLandscape ? image.Width : image.Height;

                if (maxSize > MaxSize)
                {
                    if (isLandscape)
                    {
                        scale = ((double)MaxSize/image.Width);
                        var geometry = new MagickGeometry(MaxSize, (int)scale * image.Height );
                        image.Resize(geometry);
                    }
                    else
                    {
                        scale = ((double)MaxSize/image.Height);
                        var geometry = new MagickGeometry((int)scale * image.Width, MaxSize);
                        image.Resize(geometry);
                    }
                }

                image.Format = MagickFormat.Jpg;
                image.Write(stream);
            }

            return stream.ToArray();
        }
    }
}
