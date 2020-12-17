using System;
using System.IO;
using ImageMagick;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;

namespace PhotoBank.Services.Enrichers
{
    public class PreviewEnricher : IEnricher
    {
        public Type[] Dependencies => Array.Empty<Type>();

        public void Enrich(Photo photo, SourceDataDto source)
        {
            using (var stream = new MemoryStream())
            {
                using (var image = new MagickImage(source.AbsolutePath))
                {
                    ImageHelper.CutImage(image, out var scale);
                    image.Format = MagickFormat.Jpg;
                    image.Write(stream);

                    photo.Scale = scale;
                }

                photo.PreviewImage = stream.ToArray();
            }
        }
    }
}
