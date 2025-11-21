using System;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.ImageAnalysis;
using PhotoBank.Services.Models;

namespace PhotoBank.UnitTests.Enrichers
{
    [TestFixture]
    public class CaptionEnricherTests : EnricherTestsBase<CaptionEnricher>
    {
        protected override EnricherType ExpectedEnricherType => EnricherType.Caption;
        protected override Type[] ExpectedDependencies => new[] { typeof(AnalyzeEnricher) };

        [Test]
        public async Task EnrichAsync_ShouldSetCaptions()
        {
            var photo = new Photo();
            var sourceData = new SourceDataDto
            {
                ImageAnalysis = new ImageAnalysisResult
                {
                    Description = new ImageDescription
                    {
                        Captions =
                        [
                            new ImageCaption { Text = "A beautiful sunset", Confidence = 0.95 },
                            new ImageCaption { Text = "A scenic view", Confidence = 0.85 }
                        ]
                    }
                }
            };

            await _enricher.EnrichAsync(photo, sourceData);

            photo.Captions.Should().HaveCount(2);
            photo.Captions.Should().Contain(c => c.Text == "A beautiful sunset" && c.Confidence == 0.95);
            photo.Captions.Should().Contain(c => c.Text == "A scenic view" && c.Confidence == 0.85);
        }
    }
}
