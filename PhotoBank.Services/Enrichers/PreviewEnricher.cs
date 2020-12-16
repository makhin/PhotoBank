using System;
using System.Drawing;
using System.IO;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;

namespace PhotoBank.Services.Enrichers
{
    public class PreviewEnricher : IEnricher
    {
        private readonly IComputerVisionClient _client;
        public Type[] Dependencies => Array.Empty<Type>();

        public PreviewEnricher(IComputerVisionClient client)
        {
            _client = client;
        }

        public void Enrich(Photo photo, SourceDataDto sourceData)
        {
            photo.PreviewImage = sourceData.Image;

            var thumbnail = _client.GenerateThumbnailInStreamAsync(50, 50, new MemoryStream(sourceData.Image), true).Result;
            using (var memoryStream = new MemoryStream())
            {
                thumbnail.CopyTo(memoryStream);
                photo.Thumbnail = memoryStream.ToArray();
            }
        }
    }
}
