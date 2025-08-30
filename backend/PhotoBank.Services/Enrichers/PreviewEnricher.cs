using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using Minio;
using Minio.DataModel.Args;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Enrichers
{
    public class PreviewEnricher : IEnricher
    {
        private readonly IImageService _imageService;
        private readonly IMinioClient _minio;
        public EnricherType EnricherType => EnricherType.Preview;
        public Type[] Dependencies => Array.Empty<Type>();

        public PreviewEnricher(IImageService imageService, IMinioClient minio)
        {
            _imageService = imageService;
            _minio = minio ?? throw new ArgumentNullException(nameof(minio));
        }

        public async Task EnrichAsync(Photo photo, SourceDataDto source, CancellationToken cancellationToken = default)
        {
            await using var stream = new MemoryStream();
            using (var image = new MagickImage(source.AbsolutePath))
            {
                image.AutoOrient();
                source.OriginalImage = image.Clone();
                photo.Height = image.Height;
                photo.Width = image.Width;
                photo.Orientation = (int?)image.Orientation;
                _imageService.ResizeImage(image, out var scale);
                image.Format = MagickFormat.Jpg;
                await image.WriteAsync(stream, cancellationToken);
                photo.Scale = scale;
                source.PreviewImage = image.Clone();
            }

            stream.Position = 0;
            string sha256Hex;
            using (var sha = SHA256.Create())
            {
                var hash = await sha.ComputeHashAsync(stream, cancellationToken);
                sha256Hex = Convert.ToHexString(hash);
            }

            stream.Position = 0;
            var key = $"previews/{Guid.NewGuid():N}.jpg";
            await _minio.PutObjectAsync(new PutObjectArgs()
                .WithBucket("photobank")
                .WithObject(key)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType("image/jpeg"), cancellationToken);

            var stat = await _minio.StatObjectAsync(new StatObjectArgs()
                .WithBucket("photobank")
                .WithObject(key), cancellationToken);

            photo.S3Key_Preview = key;
            photo.S3ETag_Preview = stat.ETag ?? string.Empty;
            photo.Sha256_Preview = sha256Hex;
            photo.BlobSize_Preview = stream.Length;
        }
    }
}
