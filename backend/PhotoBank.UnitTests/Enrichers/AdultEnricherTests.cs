using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Models;

namespace PhotoBank.UnitTests.Enrichers
{
    [TestFixture]
    public class AdultEnricherTests : EnricherTestsBase<AdultEnricher>
    {
        protected override EnricherType ExpectedEnricherType => EnricherType.Adult;
        protected override Type[] ExpectedDependencies => new[] { typeof(AnalyzeEnricher) };

        [Test]
        [TestCase(true, 0.95, true, 0.85)]
        [TestCase(false, 0.1, false, 0.2)]
        public async Task EnrichAsync_ShouldSetAdultProperties(bool isAdultContent, double adultScore, bool isRacyContent, double racyScore)
        {
            var photo = new Photo();
            var sourceData = new SourceDataDto
            {
                ImageAnalysis = new ImageAnalysis
                {
                    Adult = new AdultInfo
                    {
                        IsAdultContent = isAdultContent,
                        AdultScore = adultScore,
                        IsRacyContent = isRacyContent,
                        RacyScore = racyScore
                    }
                }
            };

            await _enricher.EnrichAsync(photo, sourceData);

            photo.IsAdultContent.Should().Be(isAdultContent);
            photo.AdultScore.Should().Be(adultScore);
            photo.IsRacyContent.Should().Be(isRacyContent);
            photo.RacyScore.Should().Be(racyScore);
        }
    }
}
