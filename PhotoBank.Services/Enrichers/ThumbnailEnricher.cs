using System;
using System.IO;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;

namespace PhotoBank.Services.Enrichers
{
    public class ThumbnailEnricher : IEnricher
    {
        private readonly IComputerVisionClient _client;
        public Type[] Dependencies => new Type[1]{typeof(PreviewEnricher)};

        public ThumbnailEnricher(IComputerVisionClient client)
        {
            _client = client;
        }

        public void Enrich(Photo photo, SourceDataDto sourceData)
        {
            var thumbnail = _client.GenerateThumbnailInStreamAsync(50, 50, new MemoryStream(photo.PreviewImage), true).Result;
            using (var memoryStream = new MemoryStream())
            {
                thumbnail.CopyTo(memoryStream);
                photo.Thumbnail = memoryStream.ToArray();
            }
        }
    }
}
