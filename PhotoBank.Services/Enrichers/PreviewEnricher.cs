using System;
using System.IO;
using System.Threading.Tasks;
using ImageMagick;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Enrichers
{
    public class PreviewEnricher : IEnricher
    {
        private readonly IImageService _imageService;
        public EnricherType EnricherType => EnricherType.Preview;
        public Type[] Dependencies => Array.Empty<Type>();

        public PreviewEnricher(IImageService imageService)
        {
            _imageService = imageService;
        }

        public async Task EnrichAsync(Photo photo, SourceDataDto source)
        {
            using (var stream = new MemoryStream())
            {
                using (var image = new MagickImage(source.AbsolutePath))
                {
                    image.AutoOrient();
                    source.OriginalImage = image.Clone();
                    photo.Height = image.Height;
                    photo.Width = image.Width;
                    photo.Orientation = (int?)image.Orientation;
                    _imageService.ResizeImage(image, out var scale);
                    image.Format = MagickFormat.Jpg;
                    await image.WriteAsync(stream);
                    photo.Scale = scale;
                    source.PreviewImage = image.Clone();
                }

                photo.PreviewImage = stream.ToArray();
            }
        }
    }
}
