using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Moq;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Models;

namespace PhotoBank.UnitTests.Enrichers
{
    [TestFixture]
    public class TagEnricherTests
    {
        private Mock<IRepository<Tag>> _mockTagRepository;
        private TagEnricher _tagEnricher;

        [SetUp]
        public void Setup()
        {
            _mockTagRepository = new Mock<IRepository<Tag>>();
            _tagEnricher = new TagEnricher(_mockTagRepository.Object);
        }

        [Test]
        public void EnricherType_ShouldReturnTag()
        {
            // Act
            var result = _tagEnricher.EnricherType;

            // Assert
            result.Should().Be(EnricherType.Tag);
        }

        [Test]
        public void Dependencies_ShouldReturnAnalyzeEnricher()
        {
            // Act
            var result = _tagEnricher.Dependencies;

            // Assert
            result.Should().ContainSingle()
                .And.Contain(typeof(AnalyzeEnricher));
        }

        [Test]
        public async Task EnrichAsync_ShouldAddTagsToPhoto()
        {
            // Arrange
            var photo = new Photo();
            var sourceData = new SourceDataDto
            {
                ImageAnalysis = new ImageAnalysis
                {
                    Tags = new List<ImageTag>
                    {
                        new ImageTag { Name = "Car", Confidence = 0.9 },
                        new ImageTag { Name = "Tree", Confidence = 0.8 }
                    }
                }
            };

            _mockTagRepository.Setup(r => r.GetByCondition(It.IsAny<System.Linq.Expressions.Expression<System.Func<Tag, bool>>>()
                ))
                .Returns(Enumerable.Empty<Tag>().AsQueryable());
            _mockTagRepository.Setup(r => r.InsertAsync(It.IsAny<Tag>()))
                .ReturnsAsync((Tag t) => t);

            // Act
            await _tagEnricher.EnrichAsync(photo, sourceData);

            // Assert
            photo.PhotoTags.Should().HaveCount(2);
            photo.PhotoTags.Should().Contain(pt => pt.Tag.Name == "Car" && pt.Confidence == 0.9);
            photo.PhotoTags.Should().Contain(pt => pt.Tag.Name == "Tree" && pt.Confidence == 0.8);
        }

        [Test]
        public async Task EnrichAsync_ShouldInsertNewTagIfNotExists()
        {
            // Arrange
            var photo = new Photo();
            var sourceData = new SourceDataDto
            {
                ImageAnalysis = new ImageAnalysis
                {
                    Tags = new List<ImageTag>
                    {
                        new ImageTag { Name = "Bike", Confidence = 0.7 }
                    }
                }
            };

            _mockTagRepository.Setup(r => r.GetByCondition(It.IsAny<System.Linq.Expressions.Expression<System.Func<Tag, bool>>>()
                ))
                .Returns(Enumerable.Empty<Tag>().AsQueryable());
            _mockTagRepository.Setup(r => r.InsertAsync(It.IsAny<Tag>()))
                .ReturnsAsync((Tag t) => t);

            // Act
            await _tagEnricher.EnrichAsync(photo, sourceData);

            // Assert
            _mockTagRepository.Verify(repo => repo.InsertAsync(It.Is<Tag>(t => t.Name == "Bike")), Times.Once);
        }
    }
}