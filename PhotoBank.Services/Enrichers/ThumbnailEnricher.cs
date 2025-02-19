﻿using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Enrichers
{
    public class ThumbnailEnricher : IEnricher
    {
        private readonly IComputerVisionClient _client;
        public EnricherType EnricherType => EnricherType.Thumbnail;
        public Type[] Dependencies => new[]{typeof(PreviewEnricher)};

        public ThumbnailEnricher(IComputerVisionClient client)
        {
            _client = client;
        }

        public async Task EnrichAsync(Photo photo, SourceDataDto sourceData)
        {
            var thumbnail = await _client.GenerateThumbnailInStreamAsync(50, 50, new MemoryStream(photo.PreviewImage), true);
            await using (var memoryStream = new MemoryStream())
            {
                await thumbnail.CopyToAsync(memoryStream);
                photo.Thumbnail = memoryStream.ToArray();
            }
        }
    }
}
