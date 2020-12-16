using System.IO;
using ImageMagick;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using PhotoBank.Dto;

namespace PhotoBank.Services
{
    public static class ImageHelper
    {
        public static byte[] GetFace(SourceDataDto sourceData, FaceRectangle faceRectangle)
        {
            using (MagickImage image = new MagickImage(sourceData.Path))
            {
                image.AutoOrient();

                var geometry = new MagickGeometry((int)(faceRectangle.Width / sourceData.Scale), (int)(faceRectangle.Height * sourceData.Scale))
                {
                    IgnoreAspectRatio = true,
                    Y = (int)(faceRectangle.Top / sourceData.Scale),
                    X = (int)(faceRectangle.Left / sourceData.Scale)
                };
                image.Crop(geometry);

                var stream = new MemoryStream();
                image.Write(stream);
                return stream.ToArray();
            }
        }
    }
}
