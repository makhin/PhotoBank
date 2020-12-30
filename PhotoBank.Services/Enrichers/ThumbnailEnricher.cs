using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;

namespace PhotoBank.Services.Enrichers
{
    public class ThumbnailEnricher : IEnricher
    {
        private readonly IComputerVisionClient _client;
        public Type[] Dependencies => new[]{typeof(PreviewEnricher)};

        public ThumbnailEnricher(IComputerVisionClient client)
        {
            _client = client;
        }

        public async Task Enrich(Photo photo, SourceDataDto sourceData)
        {
            var thumbnail = await _client.GenerateThumbnailInStreamAsync(50, 50, new MemoryStream(photo.PreviewImage), true);
            using (var memoryStream = new MemoryStream())
            {
                await thumbnail.CopyToAsync(memoryStream);
                photo.Thumbnail = memoryStream.ToArray();
            }
        }
    }
}
