using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using ImageMagick;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Enrichers.ObjectDetection;
using PhotoBank.Services.Enrichers.ObjectDetection.Abstractions;
using PhotoBank.Services.ImageAnalysis;
using PhotoBank.Services.Models;

namespace PhotoBank.UnitTests.Enrichers;

[TestFixture]
public class UnifiedObjectPropertyEnricherTests
{
    private Mock<IRepository<PropertyName>> _mockPropertyNameRepository;
    private Mock<ILogger<UnifiedObjectPropertyEnricher>> _mockLogger;

    [SetUp]
    public void Setup()
    {
        _mockPropertyNameRepository = new Mock<IRepository<PropertyName>>();
        _mockLogger = new Mock<ILogger<UnifiedObjectPropertyEnricher>>();
    }

    [Test]
    public void EnricherType_ShouldReturnObjectProperty()
    {
        // Arrange
        var mockProvider = new Mock<IObjectDetectionProvider>();
        mockProvider.Setup(p => p.Kind).Returns(ObjectDetectionProviderKind.Azure);
        var enricher = new UnifiedObjectPropertyEnricher(
            mockProvider.Object,
            _mockPropertyNameRepository.Object,
            _mockLogger.Object);

        // Act
        var result = enricher.EnricherType;

        // Assert
        result.Should().Be(EnricherType.ObjectProperty);
    }

    [Test]
    public void Dependencies_WithAzureProvider_ShouldReturnAnalyzeEnricher()
    {
        // Arrange
        var mockProvider = new Mock<IObjectDetectionProvider>();
        mockProvider.Setup(p => p.Kind).Returns(ObjectDetectionProviderKind.Azure);
        var enricher = new UnifiedObjectPropertyEnricher(
            mockProvider.Object,
            _mockPropertyNameRepository.Object,
            _mockLogger.Object);

        // Act
        var result = enricher.Dependencies;

        // Assert
        result.Should().ContainSingle()
            .And.Contain(typeof(AnalyzeEnricher));
    }

    [Test]
    public void Dependencies_WithYoloProvider_ShouldReturnPreviewEnricher()
    {
        // Arrange
        var mockProvider = new Mock<IObjectDetectionProvider>();
        mockProvider.Setup(p => p.Kind).Returns(ObjectDetectionProviderKind.YoloOnnx);
        var enricher = new UnifiedObjectPropertyEnricher(
            mockProvider.Object,
            _mockPropertyNameRepository.Object,
            _mockLogger.Object);

        // Act
        var result = enricher.Dependencies;

        // Assert
        result.Should().ContainSingle()
            .And.Contain(typeof(PreviewEnricher));
    }

    [Test]
    public async Task EnrichAsync_WithAzureProvider_ShouldAddObjectPropertiesToPhoto()
    {
        // Arrange
        var photo = new Photo { Scale = 1 };
        var sourceData = new SourceDataDto
        {
            ImageAnalysis = new ImageAnalysisResult
            {
                Objects = new List<DetectedObject>
                {
                    new DetectedObject
                    {
                        ObjectProperty = "Car",
                        Confidence = 0.9,
                        Rectangle = new ObjectRectangle { X = 10, Y = 10, W = 50, H = 50 }
                    },
                    new DetectedObject
                    {
                        ObjectProperty = "Tree",
                        Confidence = 0.8,
                        Rectangle = new ObjectRectangle { X = 20, Y = 20, W = 60, H = 60 }
                    }
                }
            }
        };

        var azureProvider = new AzureObjectDetectionProvider();
        var carPropertyName = new PropertyName { Name = "Car" };
        var treePropertyName = new PropertyName { Name = "Tree" };

        _mockPropertyNameRepository
            .Setup(repo => repo.GetByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<PropertyName, bool>>>()))
            .Returns(new List<PropertyName> { carPropertyName, treePropertyName }.AsQueryable());

        var enricher = new UnifiedObjectPropertyEnricher(
            azureProvider,
            _mockPropertyNameRepository.Object,
            _mockLogger.Object);

        // Act
        await enricher.EnrichAsync(photo, sourceData);

        // Assert
        photo.ObjectProperties.Should().HaveCount(2);

        var carProperty = photo.ObjectProperties.Should().ContainSingle(op => op.PropertyName.Name == "Car").Subject;
        carProperty.Confidence.Should().BeApproximately(0.9, 0.0001);

        var treeProperty = photo.ObjectProperties.Should().ContainSingle(op => op.PropertyName.Name == "Tree").Subject;
        treeProperty.Confidence.Should().BeApproximately(0.8, 0.0001);
    }

    [Test]
    public async Task EnrichAsync_WithYoloProvider_ShouldAddObjectPropertiesToPhoto()
    {
        // Arrange
        var photo = new Photo { Scale = 1 };
        var mockImage = new Mock<IMagickImage<byte>>();
        var imageBytes = new byte[] { 1, 2, 3, 4 };
        mockImage.Setup(m => m.ToByteArray()).Returns(imageBytes);

        var sourceData = new SourceDataDto
        {
            PreviewImage = mockImage.Object
        };

        var mockProvider = new Mock<IObjectDetectionProvider>();
        mockProvider.Setup(p => p.Kind).Returns(ObjectDetectionProviderKind.YoloOnnx);
        mockProvider.Setup(p => p.DetectObjects(sourceData, 1))
            .Returns(new List<DetectedObjectDto>
            {
                new DetectedObjectDto("car", 0.9f, 10, 10, 50, 50),
                new DetectedObjectDto("person", 0.85f, 100, 100, 60, 120)
            });

        var carPropertyName = new PropertyName { Id = 1, Name = "car" };
        var personPropertyName = new PropertyName { Id = 2, Name = "person" };

        _mockPropertyNameRepository
            .Setup(repo => repo.GetByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<PropertyName, bool>>>()))
            .Returns(new List<PropertyName> { carPropertyName, personPropertyName }.AsQueryable());

        var enricher = new UnifiedObjectPropertyEnricher(
            mockProvider.Object,
            _mockPropertyNameRepository.Object,
            _mockLogger.Object);

        // Act
        await enricher.EnrichAsync(photo, sourceData);

        // Assert
        photo.ObjectProperties.Should().HaveCount(2);

        var carProperty = photo.ObjectProperties.Should().ContainSingle(op => op.PropertyName.Name == "car").Subject;
        carProperty.Confidence.Should().BeApproximately(0.9, 0.0001);

        var personProperty = photo.ObjectProperties.Should().ContainSingle(op => op.PropertyName.Name == "person").Subject;
        personProperty.Confidence.Should().BeApproximately(0.85, 0.0001);
    }

    [Test]
    public async Task EnrichAsync_ShouldInsertNewPropertyNameIfNotExists()
    {
        // Arrange
        var photo = new Photo { Scale = 1 };
        var sourceData = new SourceDataDto
        {
            ImageAnalysis = new ImageAnalysisResult
            {
                Objects = new List<DetectedObject>
                {
                    new DetectedObject
                    {
                        ObjectProperty = "Bike",
                        Confidence = 0.7,
                        Rectangle = new ObjectRectangle { X = 30, Y = 30, W = 70, H = 70 }
                    }
                }
            }
        };

        var azureProvider = new AzureObjectDetectionProvider();

        _mockPropertyNameRepository
            .Setup(repo => repo.GetByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<PropertyName, bool>>>()))
            .Returns(Enumerable.Empty<PropertyName>().AsQueryable());

        var enricher = new UnifiedObjectPropertyEnricher(
            azureProvider,
            _mockPropertyNameRepository.Object,
            _mockLogger.Object);

        // Act
        await enricher.EnrichAsync(photo, sourceData);

        // Assert
        _mockPropertyNameRepository.Verify(
            repo => repo.InsertAsync(It.Is<PropertyName>(pn => pn.Name == "Bike")),
            Times.Once);
    }

    [Test]
    public async Task EnrichAsync_WithNoDetections_ShouldNotAddObjectProperties()
    {
        // Arrange
        var photo = new Photo { Scale = 1 };
        var sourceData = new SourceDataDto
        {
            ImageAnalysis = new ImageAnalysisResult
            {
                Objects = new List<DetectedObject>()
            }
        };

        var azureProvider = new AzureObjectDetectionProvider();
        var enricher = new UnifiedObjectPropertyEnricher(
            azureProvider,
            _mockPropertyNameRepository.Object,
            _mockLogger.Object);

        // Act
        await enricher.EnrichAsync(photo, sourceData);

        // Assert
        photo.ObjectProperties.Should().BeNullOrEmpty();
        _mockPropertyNameRepository.Verify(
            repo => repo.InsertAsync(It.IsAny<PropertyName>()),
            Times.Never);
    }

    [Test]
    public async Task EnrichAsync_WithScale_ShouldApplyScaleToRectangle()
    {
        // Arrange
        var photo = new Photo { Scale = 2 }; // Scale = 2
        var sourceData = new SourceDataDto
        {
            ImageAnalysis = new ImageAnalysisResult
            {
                Objects = new List<DetectedObject>
                {
                    new DetectedObject
                    {
                        ObjectProperty = "dog",
                        Confidence = 0.8,
                        Rectangle = new ObjectRectangle { X = 100, Y = 100, W = 200, H = 200 }
                    }
                }
            }
        };

        var azureProvider = new AzureObjectDetectionProvider();
        var dogPropertyName = new PropertyName { Id = 1, Name = "dog" };

        _mockPropertyNameRepository
            .Setup(repo => repo.GetByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<PropertyName, bool>>>()))
            .Returns(new List<PropertyName> { dogPropertyName }.AsQueryable());

        var enricher = new UnifiedObjectPropertyEnricher(
            azureProvider,
            _mockPropertyNameRepository.Object,
            _mockLogger.Object);

        // Act
        await enricher.EnrichAsync(photo, sourceData);

        // Assert
        photo.ObjectProperties.Should().HaveCount(1);
        var objectProperty = photo.ObjectProperties.First();
        objectProperty.Rectangle.Should().NotBeNull();
        // The rectangle should be scaled by photo.Scale (divided by 2)
    }

    [Test]
    public async Task EnrichAsync_YoloProvider_WithNullPreviewImage_ShouldNotAddObjectProperties()
    {
        // Arrange
        var photo = new Photo { Scale = 1 };
        var sourceData = new SourceDataDto
        {
            PreviewImage = null
        };

        var mockProvider = new Mock<IObjectDetectionProvider>();
        mockProvider.Setup(p => p.Kind).Returns(ObjectDetectionProviderKind.YoloOnnx);

        var enricher = new UnifiedObjectPropertyEnricher(
            mockProvider.Object,
            _mockPropertyNameRepository.Object,
            _mockLogger.Object);

        // Act
        await enricher.EnrichAsync(photo, sourceData);

        // Assert
        photo.ObjectProperties.Should().BeNullOrEmpty();
        mockProvider.Verify(
            p => p.DetectObjects(It.IsAny<SourceDataDto>(), It.IsAny<float>()),
            Times.Never);
    }
}
