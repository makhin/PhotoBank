using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.ImageAnalysis;
using PhotoBank.Services.Models;
using ImageMagick;

namespace PhotoBank.UnitTests.Enrichers
{
    [TestFixture]
    public class AnalyzeEnricherTests
    {
        private Mock<IImageAnalyzer> _mockAnalyzer;
        private AnalyzeEnricher _analyzeEnricher;

        [SetUp]
        public void Setup()
        {
            _mockAnalyzer = new Mock<IImageAnalyzer>();
            _analyzeEnricher = new AnalyzeEnricher(_mockAnalyzer.Object);
        }

        [Test]
        public void EnricherType_ShouldReturnAnalyze()
        {
            // Act
            var result = _analyzeEnricher.EnricherType;

            // Assert
            result.Should().Be(EnricherType.Analyze);
        }

        [Test]
        public void Dependencies_ShouldReturnAdultEnricher()
        {
            // Act
            var result = _analyzeEnricher.Dependencies;

            // Assert
            result.Should().ContainSingle()
                .And.Contain(typeof(AdultEnricher));
        }

        [Test]
        public async Task EnrichAsync_ShouldSetImageAnalysis()
        {
            // Arrange
            var photo = new Photo();
            var preview = new MagickImage(MagickColors.Red, 10, 10) { Format = MagickFormat.Jpeg };
            var sourceData = new SourceDataDto
            {
                PreviewImage = preview
            };
            var imageAnalysisResult = new ImageAnalysisResult
            {
                Adult = new AdultContent
                {
                    IsAdultContent = true,
                    AdultScore = 0.95,
                    IsRacyContent = true,
                    RacyScore = 0.85
                }
            };

            _mockAnalyzer
                .Setup(a => a.AnalyzeAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(imageAnalysisResult);

            // Act
            await _analyzeEnricher.EnrichAsync(photo, sourceData);

            // Assert
            sourceData.ImageAnalysis.Should().Be(imageAnalysisResult);
            _mockAnalyzer.Verify(a => a.AnalyzeAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnrichAsync_ShouldSkipIfImageAnalysisAlreadySet()
        {
            // Arrange
            var photo = new Photo();
            var preview = new MagickImage(MagickColors.Red, 10, 10) { Format = MagickFormat.Jpeg };
            var existingResult = new ImageAnalysisResult();
            var sourceData = new SourceDataDto
            {
                PreviewImage = preview,
                ImageAnalysis = existingResult
            };

            // Act
            await _analyzeEnricher.EnrichAsync(photo, sourceData);

            // Assert
            sourceData.ImageAnalysis.Should().Be(existingResult);
            _mockAnalyzer.Verify(a => a.AnalyzeAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnrichAsync_ShouldSkipIfNoPreviewImage()
        {
            // Arrange
            var photo = new Photo();
            var sourceData = new SourceDataDto
            {
                PreviewImage = null
            };

            // Act
            await _analyzeEnricher.EnrichAsync(photo, sourceData);

            // Assert
            sourceData.ImageAnalysis.Should().BeNull();
            _mockAnalyzer.Verify(a => a.AnalyzeAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
