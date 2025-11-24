using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.ImageAnalysis;
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
                ImageAnalysis = new ImageAnalysisResult
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
                ImageAnalysis = new ImageAnalysisResult
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

        [Test]
        public async Task EnrichAsync_ShouldResetIncomingIdBeforeInsert()
        {
            // Arrange
            var photo = new Photo();
            var sourceData = new SourceDataDto
            {
                ImageAnalysis = new ImageAnalysisResult
                {
                    Tags = new List<ImageTag>
                    {
                        new ImageTag { Name = "Bike", Confidence = 0.7 }
                    }
                }
            };

            _mockTagRepository
                .Setup(r => r.GetByCondition(It.IsAny<System.Linq.Expressions.Expression<System.Func<Tag, bool>>>()
                ))
                .Returns(Enumerable.Empty<Tag>().AsQueryable());

            Tag? inserted = null;
            _mockTagRepository.Setup(r => r.InsertAsync(It.IsAny<Tag>()))
                .Callback<Tag>(t => inserted = t)
                .ReturnsAsync((Tag t) => t);

            var enricher = new IncomingIdTagEnricher(_mockTagRepository.Object);

            // Act
            await enricher.EnrichAsync(photo, sourceData);

            // Assert
            inserted.Should().NotBeNull();
            inserted!.Id.Should().Be(0);
            inserted.Name.Should().Be("Bike");
        }

        private sealed class IncomingIdTagEnricher : BaseLookupEnricher<Tag, PhotoTag>
        {
            public IncomingIdTagEnricher(IRepository<Tag> repo)
                : base(
                    repo,
                    src => src.ImageAnalysis?.Tags?.Select(t => t.Name) ?? Enumerable.Empty<string>(),
                    model => model.Name,
                    name => new Tag { Id = 123, Name = name, Hint = string.Empty },
                    (photo, name, tagModel, src) =>
                    {
                        var tag = src.ImageAnalysis?.Tags?.FirstOrDefault(t =>
                            string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));

                        return new PhotoTag
                        {
                            Photo = photo,
                            Tag = tagModel,
                            Confidence = tag?.Confidence ?? 0
                        };
                    })
            {
            }

            public override EnricherType EnricherType => EnricherType.Tag;

            protected override ICollection<PhotoTag> GetCollection(Photo photo) =>
                photo.PhotoTags ??= new List<PhotoTag>();
        }
    }
}
