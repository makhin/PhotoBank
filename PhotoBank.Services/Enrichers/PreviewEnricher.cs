﻿using System;
using System.IO;
using System.Threading.Tasks;
using ImageMagick;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;

namespace PhotoBank.Services.Enrichers
{
    public class PreviewEnricher : IEnricher
    {
        public Type[] Dependencies => Array.Empty<Type>();

        public async Task Enrich(Photo photo, SourceDataDto source)
        {
            using (var stream = new MemoryStream())
            {
                using (var image = new MagickImage(source.AbsolutePath))
                {
                    image.AutoOrient();
                    photo.Height = image.Height;
                    photo.Width = image.Width;
                    photo.Orientation = (int?)image.Orientation;
                    ImageHelper.ResizeImage(image, out var scale);
                    image.Format = MagickFormat.Jpg;
                    await image.WriteAsync(stream);
                    photo.Scale = scale;
                    

                }

                photo.PreviewImage = stream.ToArray();
            }
        }
    }
}
