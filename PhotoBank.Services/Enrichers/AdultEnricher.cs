﻿using System;
using System.Threading.Tasks;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Enrichers
{
    public class AdultEnricher : IEnricher
    {
        public EnricherType EnricherType => EnricherType.Adult;

        public Type[] Dependencies => new Type[1] { typeof(AnalyzeEnricher) };

        public async Task EnrichAsync(Photo photo, SourceDataDto sourceData)
        {
            await Task.Run(() =>
            {
                photo.IsAdultContent = sourceData.ImageAnalysis.Adult.IsAdultContent;
                photo.AdultScore = sourceData.ImageAnalysis.Adult.AdultScore;
                photo.IsRacyContent = sourceData.ImageAnalysis.Adult.IsRacyContent;
                photo.RacyScore = sourceData.ImageAnalysis.Adult.RacyScore;
            });
        }
    }
}
