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
    public class ColorEnricherTests : EnricherTestsBase<ColorEnricher>
    {
        protected override EnricherType ExpectedEnricherType => EnricherType.Color;
        protected override Type[] ExpectedDependencies => new[] { typeof(AnalyzeEnricher) };

        [TestCase(true, "FF5733", "Black", "White", new[] { "Black", "White", "Gray" })]
        [TestCase(false, "00FF00", "Red", "Blue", new[] { "Red", "Blue", "Green" })]
        public async Task EnrichAsync_ShouldSetColorProperties(bool isBWImg, string accentColor, string dominantColorBackground,
            string dominantColorForeground, string[] dominantColors)
        {
            var photo = new Photo();
            var sourceData = new SourceDataDto
            {
                ImageAnalysis = new ImageAnalysis
                {
                    Color = new ColorInfo
                    {
                        IsBWImg = isBWImg,
                        AccentColor = accentColor,
                        DominantColorBackground = dominantColorBackground,
                        DominantColorForeground = dominantColorForeground,
                        DominantColors = dominantColors
                    }
                }
            };

            await _enricher.EnrichAsync(photo, sourceData);

            photo.IsBW.Should().Be(isBWImg);
            photo.AccentColor.Should().Be(accentColor);
            photo.DominantColorBackground.Should().Be(dominantColorBackground);
            photo.DominantColorForeground.Should().Be(dominantColorForeground);
            photo.DominantColors.Should().Be(string.Join(",", dominantColors));
        }
    }
}
