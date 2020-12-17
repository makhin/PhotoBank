using System;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;

namespace PhotoBank.Services.Enrichers
{
    public class AdultEnricher : IEnricher
    {
        public Type[] Dependencies => new Type[1] { typeof(AnalyzeEnricher) };

        public void Enrich(Photo photo, SourceDataDto sourceData)
        {
            photo.IsAdultContent = sourceData.ImageAnalysis.Adult.IsAdultContent;
            photo.AdultScore = sourceData.ImageAnalysis.Adult.AdultScore;
            photo.IsRacyContent = sourceData.ImageAnalysis.Adult.IsRacyContent;
            photo.RacyScore = sourceData.ImageAnalysis.Adult.RacyScore;
        }
    }
}
