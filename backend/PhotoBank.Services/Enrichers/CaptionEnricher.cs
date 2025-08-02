using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Enrichers
{
    public class CaptionEnricher : IEnricher
    {
        public EnricherType EnricherType => EnricherType.Caption;
        public Type[] Dependencies => new Type[1] { typeof(AnalyzeEnricher) };

        public Task EnrichAsync(Photo photo, SourceDataDto sourceData)
        {
            photo.Captions = new List<Caption>();
            foreach (var caption in sourceData.ImageAnalysis.Description.Captions)
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
