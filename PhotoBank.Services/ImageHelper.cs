using System.IO;
using ImageMagick;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace PhotoBank.Services
{
    public static class ImageHelper
    {
        public static byte[] GetFace(byte[] data, FaceRectangle faceRectangle)
        {
            using (var image = new MagickImage(data))
            {
                MagickGeometry size = new MagickGeometry(faceRectangle.Width, faceRectangle.Height);
                size.IgnoreAspectRatio = true;
                size.Y = image.Height/2 - faceRectangle.Height/2;
                size.X = image.Width/2 - faceRectangle.Width/2;
                image.Crop(size);
                var stream = new MemoryStream();
                image.Write(stream);
                return stream.ToArray();
            }
        }
    }
}
