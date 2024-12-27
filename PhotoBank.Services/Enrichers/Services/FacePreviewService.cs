using System.IO;
using System.Threading.Tasks;
using ImageMagick;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

namespace PhotoBank.Services.Enrichers.Services
{
    public class FacePreviewService : IFacePreviewService
    {
        public async Task<byte[]> CreateFacePreview(DetectedFace detectedFace, IMagickImage<byte> image, double photoScale)
        {
            await using (var stream = new MemoryStream())
            {
                var faceImage = image.Clone();
                faceImage.Crop(GetMagickGeometry(detectedFace, photoScale));
                await faceImage.WriteAsync(stream);
                return stream.ToArray();
            }
        }

        private static MagickGeometry GetMagickGeometry(DetectedFace detectedFace, double photoScale)
        {
            var height = (int)(detectedFace.FaceRectangle.Height / photoScale);
            var width = (int)(detectedFace.FaceRectangle.Width / photoScale);
            var top = (int)(detectedFace.FaceRectangle.Top / photoScale);
            var left = (int)(detectedFace.FaceRectangle.Left / photoScale);

            var geometry = new MagickGeometry(width, height)
            {
                IgnoreAspectRatio = true,
                Y = top,
                X = left
            };
            return geometry;
        }
    }
}
