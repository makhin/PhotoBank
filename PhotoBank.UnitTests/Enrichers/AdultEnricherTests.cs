using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto.Load;
using PhotoBank.Services.Enrichers;

namespace PhotoBank.UnitTests.Enrichers
{
    [TestFixture]
    public class AdultEnricherTests
    {
        private AdultEnricher _adultEnricher;

        [SetUp]
        public void Setup()
        {
            _adultEnricher = new AdultEnricher();
        }

        [Test]
        public void EnricherType_ShouldReturnAdult()
        {
            // Act
            var result = _adultEnricher.EnricherType;

            // Assert
            result.Should().Be(EnricherType.Adult);
        }

        [Test]
        public void Dependencies_ShouldReturnAnalyzeEnricher()
        {
            // Act
            var result = _adultEnricher.Dependencies;

            // Assert
            result.Should().ContainSingle()
                .And.Contain(typeof(AnalyzeEnricher));
        }

        [Test]
        [TestCase(true, 0.95, true, 0.85)]
        [TestCase(false, 0.1, false, 0.2)]
        public async Task EnrichAsync_ShouldSetAdultProperties(bool isAdultContent, double adultScore, bool isRacyContent, double racyScore)
        {
            // Arrange
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

            // Act
            await _adultEnricher.EnrichAsync(photo, sourceData);

            // Assert
            photo.IsAdultContent.Should().Be(isAdultContent);
            photo.AdultScore.Should().Be(adultScore);
            photo.IsRacyContent.Should().Be(isRacyContent);
            photo.RacyScore.Should().Be(racyScore);
        }
    }
}
