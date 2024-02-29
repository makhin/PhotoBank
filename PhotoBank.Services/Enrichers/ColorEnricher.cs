﻿using System;
using System.Threading.Tasks;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto.Load;

namespace PhotoBank.Services.Enrichers
{
    public class ColorEnricher : IEnricher
    {
        public EnricherType EnricherType => EnricherType.Color;
        public bool IsActive { get; set; }
        public Type[] Dependencies => new[] { typeof(AnalyzeEnricher) };

        public async Task EnrichAsync(Photo photo, SourceDataDto sourceData)
        {
            if (!IsActive) return;
            await Task.Run(() =>
            {
                photo.IsBW = sourceData.ImageAnalysis.Color.IsBWImg;
                photo.AccentColor = sourceData.ImageAnalysis.Color.AccentColor;
                photo.DominantColorBackground = sourceData.ImageAnalysis.Color.DominantColorBackground;
                photo.DominantColorForeground = sourceData.ImageAnalysis.Color.DominantColorForeground;
                photo.DominantColors = string.Join(",", sourceData.ImageAnalysis.Color.DominantColors);

            });
        }
    }
}
