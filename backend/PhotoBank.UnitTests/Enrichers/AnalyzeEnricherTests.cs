using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Enrichers;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using PhotoBank.Services.Models;

namespace PhotoBank.UnitTests.Enrichers
{
    [TestFixture]
    public class AnalyzeEnricherTests
    {
        private Mock<IComputerVisionClient> _mockClient;
        private AnalyzeEnricher _analyzeEnricher;

        [SetUp]
        public void Setup()
        {
            _mockClient = new Mock<IComputerVisionClient>();
            _analyzeEnricher = new AnalyzeEnricher(_mockClient.Object);
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
        public void Dependencies_ShouldReturnPreviewEnricher()
        {
            // Act
            var result = _analyzeEnricher.Dependencies;

            // Assert
            result.Should().ContainSingle()
                .And.Contain(typeof(PreviewEnricher));
        }

        [Test]
        [Ignore("Need to fix")]
        public async Task EnrichAsync_ShouldSetImageAnalysis()
        {
            // Arrange
            var photo = new Photo { PreviewImage = new byte[] { 1, 2, 3 } };
            var sourceData = new SourceDataDto();
            var imageAnalysis = new ImageAnalysis
            {
                Adult = new AdultInfo
                {
                    IsAdultContent = true,
                    AdultScore = 0.95,
                    IsRacyContent = true,
                    RacyScore = 0.85
                }
            };

            _mockClient.Setup(client => client.AnalyzeImageInStreamAsync(It.IsAny<Stream>(), It.IsAny<IList<VisualFeatureTypes?>>(), It.IsAny<IList<Details?>>(), It.IsAny<string>(), It.IsAny<IList<DescriptionExclude?>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(imageAnalysis);

            // Act
            await _analyzeEnricher.EnrichAsync(photo, sourceData);

            // Assert
            sourceData.ImageAnalysis.Should().Be(imageAnalysis);
            _mockClient.Verify(client => client.AnalyzeImageInStreamAsync(It.IsAny<Stream>(), It.IsAny<IList<VisualFeatureTypes?>>(), It.IsAny<IList<Details?>>(), It.IsAny<string>(), It.IsAny<IList<DescriptionExclude?>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
