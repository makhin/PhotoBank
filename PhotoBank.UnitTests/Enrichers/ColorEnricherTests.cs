using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Moq;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto.Load;
using PhotoBank.Services.Enrichers;

namespace PhotoBank.UnitTests.Enrichers
{
    [TestFixture]
    public class ColorEnricherTests
    {
        private ColorEnricher _colorEnricher;

        [SetUp]
        public void Setup()
        {
            _colorEnricher = new ColorEnricher();
        }

        [Test]
        public void EnricherType_ShouldReturnColor()
        {
            // Act
            var result = _colorEnricher.EnricherType;

            // Assert
            result.Should().Be(EnricherType.Color);
        }

        [Test]
        public void Dependencies_ShouldReturnAnalyzeEnricher()
        {
            // Act
            var result = _colorEnricher.Dependencies;

            // Assert
            result.Should().ContainSingle()
                .And.Contain(typeof(AnalyzeEnricher));
        }

        [TestCase(true, "FF5733", "Black", "White", new[] { "Black", "White", "Gray" })]
        [TestCase(false, "00FF00", "Red", "Blue", new[] { "Red", "Blue", "Green" })]
        public async Task EnrichAsync_ShouldSetColorProperties(bool isBWImg, string accentColor, string dominantColorBackground, string dominantColorForeground, string[] dominantColors)
        {
            // Arrange
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

            // Act
            await _colorEnricher.EnrichAsync(photo, sourceData);

            // Assert
            photo.IsBW.Should().Be(isBWImg);
            photo.AccentColor.Should().Be(accentColor);
            photo.DominantColorBackground.Should().Be(dominantColorBackground);
            photo.DominantColorForeground.Should().Be(dominantColorForeground);
            photo.DominantColors.Should().Be(string.Join(",", dominantColors));
        }
    }
}

