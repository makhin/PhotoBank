using System;
using System.Threading;
using System.Threading.Tasks;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Enrichers
{
    public class AdultEnricher : IEnricher
    {
        public EnricherType EnricherType => EnricherType.Adult;

        public Type[] Dependencies => new Type[1] { typeof(AnalyzeEnricher) };

        public Task EnrichAsync(Photo photo, SourceDataDto sourceData, CancellationToken cancellationToken = default)
        {
            var adult = sourceData.ImageAnalysis?.Adult;
            if (adult == null) return Task.CompletedTask;

            photo.IsAdultContent = adult.IsAdultContent;
            photo.AdultScore = adult.AdultScore;
            photo.IsRacyContent = adult.IsRacyContent;
            photo.RacyScore = adult.RacyScore;
            return Task.CompletedTask;
        }
    }
}
