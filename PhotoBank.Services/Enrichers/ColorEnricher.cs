using System;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;

namespace PhotoBank.Services.Enrichers
{
    public class ColorEnricher : IEnricher
    {
        public Type[] Dependencies => new Type[1] { typeof(AnalyzeEnricher) };

        public void Enrich(Photo photo, SourceDataDto sourceData)

        {
            photo.IsBW = sourceData.ImageAnalysis.Color.IsBWImg;
            photo.AccentColor = sourceData.ImageAnalysis.Color.AccentColor;
            photo.DominantColorBackground = sourceData.ImageAnalysis.Color.DominantColorBackground;
            photo.DominantColorForeground = sourceData.ImageAnalysis.Color.DominantColorForeground;
            photo.DominantColors = string.Join(",", sourceData.ImageAnalysis.Color.DominantColors);
        }
    }
}
