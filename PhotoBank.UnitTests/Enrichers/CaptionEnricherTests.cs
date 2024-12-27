using System;
using System.Collections.Generic;
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
    public class CaptionEnricherTests
    {
        private CaptionEnricher _captionEnricher;

        [SetUp]
        public void Setup()
        {
            _captionEnricher = new CaptionEnricher();
        }

        [Test]
        public void EnricherType_ShouldReturnCaption()
        {
            // Act
            var result = _captionEnricher.EnricherType;

            // Assert
            result.Should().Be(EnricherType.Caption);
        }

        [Test]
        public void Dependencies_ShouldReturnAnalyzeEnricher()
        {
            // Act
            var result = _captionEnricher.Dependencies;

            // Assert
            result.Should().ContainSingle()
                .And.Contain(typeof(AnalyzeEnricher));
        }

        [Test]
        public async Task EnrichAsync_ShouldSetCaptions()
        {
            // Arrange
            var photo = new Photo();
            var sourceData = new SourceDataDto
            {
                ImageAnalysis = new ImageAnalysis
                {
                    Description = new ImageDescriptionDetails
                    {
                        Captions = new List<ImageCaption>
                        {
                            new ImageCaption { Text = "A beautiful sunset", Confidence = 0.95 },
                            new ImageCaption { Text = "A scenic view", Confidence = 0.85 }
                        }
                    }
                }
            };

            // Act
            await _captionEnricher.EnrichAsync(photo, sourceData);

            // Assert
            photo.Captions.Should().HaveCount(2);
            photo.Captions.Should().Contain(c => c.Text == "A beautiful sunset" && c.Confidence == 0.95);
            photo.Captions.Should().Contain(c => c.Text == "A scenic view" && c.Confidence == 0.85);
        }
    }
}

