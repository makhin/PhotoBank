using System;
using System.Threading;
using System.Threading.Tasks;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Enrichers
{
    public class ColorEnricher : IEnricher
    {
        public EnricherType EnricherType => EnricherType.Color;
        public Type[] Dependencies => [typeof(AnalyzeEnricher)];

        public Task EnrichAsync(Photo photo, SourceDataDto sourceData, CancellationToken cancellationToken = default)
        {
            var color = sourceData.ImageAnalysis?.Color;
            if (color == null) return Task.CompletedTask;

            photo.IsBW = color.IsBWImg;
            photo.AccentColor = color.AccentColor;
            photo.DominantColorBackground = color.DominantColorBackground;
            photo.DominantColorForeground = color.DominantColorForeground;
            photo.DominantColors = string.Join(",", color.DominantColors);
            return Task.CompletedTask;
        }
    }
}
