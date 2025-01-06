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
    public class ObjectPropertyEnricherTests
    {
        private Mock<IRepository<PropertyName>> _mockPropertyNameRepository;
        private ObjectPropertyEnricher _objectPropertyEnricher;

        [SetUp]
        public void Setup()
        {
            _mockPropertyNameRepository = new Mock<IRepository<PropertyName>>();
            _objectPropertyEnricher = new ObjectPropertyEnricher(_mockPropertyNameRepository.Object);
        }

        [Test]
        public void EnricherType_ShouldReturnObjectProperty()
        {
            // Act
            var result = _objectPropertyEnricher.EnricherType;

            // Assert
            result.Should().Be(EnricherType.ObjectProperty);
        }

        [Test]
        public void Dependencies_ShouldReturnAnalyzeEnricher()
        {
            // Act
            var result = _objectPropertyEnricher.Dependencies;

            // Assert
            result.Should().ContainSingle()
                .And.Contain(typeof(AnalyzeEnricher));
        }

        [Test]
        public async Task EnrichAsync_ShouldAddObjectPropertiesToPhoto()
        {
            // Arrange
            var photo = new Photo { Scale = 1 };
            var sourceData = new SourceDataDto
            {
                ImageAnalysis = new ImageAnalysis
                {
                    Objects = new List<DetectedObject>
                    {
                        new DetectedObject(new BoundingRect { X = 10, Y = 10, W = 50, H = 50 }, "Car", 0.9),
                        new DetectedObject(new BoundingRect { X = 20, Y = 20, W = 60, H = 60 }, "Tree", 0.8)
                    }
                }
            };

            var carPropertyName = new PropertyName { Name = "Car" };
            var treePropertyName = new PropertyName { Name = "Tree" };

            _mockPropertyNameRepository.Setup(repo => repo.GetByCondition(It.IsAny<System.Linq.Expressions.Expression<System.Func<PropertyName, bool>>>()))
                .Returns((System.Linq.Expressions.Expression<System.Func<PropertyName, bool>> predicate) =>
                {
                    var propertyNames = new List<PropertyName> { carPropertyName, treePropertyName };
                    return propertyNames.AsQueryable().Where(predicate).AsQueryable();
                });

            // Act
            await _objectPropertyEnricher.EnrichAsync(photo, sourceData);

            // Assert
            photo.ObjectProperties.Should().HaveCount(2);
            photo.ObjectProperties.Should().Contain(op => op.PropertyName.Name == "Car" && op.Confidence == 0.9);
            photo.ObjectProperties.Should().Contain(op => op.PropertyName.Name == "Tree" && op.Confidence == 0.8);
        }

        [Test]
        public async Task EnrichAsync_ShouldInsertNewPropertyNameIfNotExists()
        {
            // Arrange
            var photo = new Photo { Scale = 1 };
            var sourceData = new SourceDataDto
            {
                ImageAnalysis = new ImageAnalysis
                {
                    Objects = new List<DetectedObject>
                    {
                        new DetectedObject(new BoundingRect() { X = 30, Y = 30, W = 70, H = 70 }, "Bike", 0.7)
                    }
                }
            };

            _mockPropertyNameRepository.Setup(repo => repo.GetByCondition(It.IsAny<System.Linq.Expressions.Expression<System.Func<PropertyName, bool>>>()))
                .Returns(Enumerable.Empty<PropertyName>().AsQueryable());

            // Act
            await _objectPropertyEnricher.EnrichAsync(photo, sourceData);

            // Assert
            _mockPropertyNameRepository.Verify(repo => repo.InsertAsync(It.Is<PropertyName>(pn => pn.Name == "Bike")), Times.Once);
        }
    }
}


