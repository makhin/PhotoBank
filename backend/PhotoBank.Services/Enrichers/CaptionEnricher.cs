using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Enrichers
{
    public class CaptionEnricher : IEnricher
    {
        public EnricherType EnricherType => EnricherType.Caption;
        public Type[] Dependencies => [typeof(AnalyzeEnricher)];

        public Task EnrichAsync(Photo photo, SourceDataDto sourceData, CancellationToken cancellationToken = default)
        {
            var captions = sourceData.ImageAnalysis?.Description?.Captions;
            if (captions == null || captions.Count == 0)
                return Task.CompletedTask;

            photo.Captions = new List<Caption>();

            foreach (var caption in captions)
            {
                photo.Captions.Add(new Caption
                {
                    Confidence = caption.Confidence,
                    Text = caption.Text
                });
            }

            return Task.CompletedTask;
        }
    }
}
