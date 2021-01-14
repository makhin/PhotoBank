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
