using System;
using System.Threading.Tasks;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;

namespace PhotoBank.Services.Enrichers
{
    public class AdultEnricher : IEnricher
    {
        public Type[] Dependencies => new Type[1] { typeof(AnalyzeEnricher) };

        public async Task Enrich(Photo photo, SourceDataDto sourceData)
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
