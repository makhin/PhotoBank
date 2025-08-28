using System;
using System.IO;
using System.Threading.Tasks;
using ImageMagick;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Minio;
using Minio.DataModel.Args;

namespace PhotoBank.Services.Enrichers.Services
{
    public class FacePreviewService : IFacePreviewService
    {
        private readonly IMinioClient _minio;

        public FacePreviewService(IMinioClient minio)
        {
            _minio = minio ?? throw new ArgumentNullException(nameof(minio));
        }

        public async Task<(string key, string etag)> CreateFacePreview(DetectedFace detectedFace, IMagickImage<byte> image, double photoScale)
        {
            await using var stream = new MemoryStream();
            var faceImage = image.Clone();
            faceImage.Crop(GetMagickGeometry(detectedFace, photoScale));
            await faceImage.WriteAsync(stream);
            stream.Position = 0;

            var key = $"faces/{Guid.NewGuid():N}.jpg";
            await _minio.PutObjectAsync(new PutObjectArgs()
                .WithBucket("photobank")
                .WithObject(key)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType("image/jpeg"));

            var stat = await _minio.StatObjectAsync(new StatObjectArgs()
                .WithBucket("photobank")
                .WithObject(key));

            return (key, stat.ETag ?? string.Empty);
        }

        private static MagickGeometry GetMagickGeometry(DetectedFace detectedFace, double photoScale)
        {
            var height = (uint)(detectedFace.FaceRectangle.Height / photoScale);
            var width = (uint)(detectedFace.FaceRectangle.Width / photoScale);
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
