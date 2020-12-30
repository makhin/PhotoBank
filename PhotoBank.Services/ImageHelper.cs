using System.IO;
using System.Threading.Tasks;
using ImageMagick;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using PhotoBank.Dto;

namespace PhotoBank.Services
{
    // TODO Refactor to ImageService

    public static class ImageHelper
    {
        public const int MaxSize = 1920;

        public static async Task<byte[]> GetFace(string absolutePath, double scale, FaceRectangle faceRectangle)
        {
            using (MagickImage image = new MagickImage(absolutePath))
            {
                image.AutoOrient();

                var geometry = new MagickGeometry((int)(faceRectangle.Width / scale), (int)(faceRectangle.Height / scale))
                {
                    IgnoreAspectRatio = true,
                    Y = (int)(faceRectangle.Top / scale),
                    X = (int)(faceRectangle.Left / scale)
                };
                image.Crop(geometry);
                ResizeImage(image, out _);

                var stream = new MemoryStream();
                image.Format = MagickFormat.Jpg;
                await image.WriteAsync(stream);
                return stream.ToArray();
            }
        }

        public static void ResizeImage(MagickImage image, out double scale)
        {
            var isLandscape = image.Width > image.Height;
            var maxSize = isLandscape ? image.Width : image.Height;
            scale = 1;

            if (maxSize <= MaxSize) return;

            if (isLandscape)
            {
                scale = ((double)MaxSize / image.Width);
                var geometry = new MagickGeometry(MaxSize, (int)scale * image.Height);
                image.Resize(geometry);
            }
            else
            {
                scale = ((double)MaxSize / image.Height);
                var geometry = new MagickGeometry((int)scale * image.Width, MaxSize);
                image.Resize(geometry);
            }
        }
    }
}
