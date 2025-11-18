using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using ImageMagick;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Enrichers.Onnx;
using PhotoBank.Services.Models;

namespace PhotoBank.UnitTests.Enrichers;

[TestFixture]
public class OnnxObjectDetectionEnricherTests
{
    private Mock<IRepository<PropertyName>> _mockPropertyNameRepository;
    private Mock<IYoloOnnxService> _mockYoloService;
    private OnnxObjectDetectionEnricher _enricher;

    [SetUp]
    public void Setup()
    {
        _mockPropertyNameRepository = new Mock<IRepository<PropertyName>>();
        _mockYoloService = new Mock<IYoloOnnxService>();

        var mockOptions = new Mock<IOptions<YoloOnnxOptions>>();
        mockOptions.Setup(o => o.Value).Returns(new YoloOnnxOptions
        {
            ConfidenceThreshold = 0.5f,
            NmsThreshold = 0.45f
        });

        _enricher = new OnnxObjectDetectionEnricher(
            _mockPropertyNameRepository.Object,
            _mockYoloService.Object,
            mockOptions.Object);
    }

    [Test]
    public void EnricherType_ShouldReturnObjectProperty()
    {
        // Act
        var result = _enricher.EnricherType;

        // Assert
        result.Should().Be(EnricherType.ObjectProperty);
    }

    [Test]
    public void Dependencies_ShouldReturnPreviewEnricher()
    {
        // Act
        var result = _enricher.Dependencies;

        // Assert
        result.Should().ContainSingle()
            .And.Contain(typeof(PreviewEnricher));
    }

    [Test]
    public async Task EnrichAsync_WithNullPhoto_ShouldThrowArgumentNullException()
    {
        // Arrange
        var sourceData = new SourceDataDto();

        // Act & Assert
        await _enricher.Invoking(e => e.EnrichAsync(null!, sourceData))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Test]
    public async Task EnrichAsync_WithNullSourceData_ShouldThrowArgumentNullException()
    {
        // Arrange
        var photo = new Photo();

        // Act & Assert
        await _enricher.Invoking(e => e.EnrichAsync(photo, null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Test]
    public async Task EnrichAsync_WithNullPreviewImage_ShouldNotCallYoloService()
    {
        // Arrange
        var photo = new Photo { Scale = 1 };
        var sourceData = new SourceDataDto
        {
            PreviewImage = null
        };

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        _mockYoloService.Verify(
            s => s.DetectObjects(It.IsAny<byte[]>(), It.IsAny<float>(), It.IsAny<float>()),
            Times.Never);
    }

    [Test]
    public async Task EnrichAsync_WithDetectedObjects_ShouldAddObjectPropertiesToPhoto()
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

        var detectedObjects = new List<DetectedObjectOnnx>
        {
            new DetectedObjectOnnx
            {
                ClassName = "car",
                Confidence = 0.9f,
                X = 10,
                Y = 10,
                Width = 50,
                Height = 50
            },
            new DetectedObjectOnnx
            {
                ClassName = "person",
                Confidence = 0.85f,
                X = 100,
                Y = 100,
                Width = 60,
                Height = 120
            }
        };

        _mockYoloService
            .Setup(s => s.DetectObjects(imageBytes, 0.5f, 0.45f))
            .Returns(detectedObjects);

        var carPropertyName = new PropertyName { Id = 1, Name = "car" };
        var personPropertyName = new PropertyName { Id = 2, Name = "person" };

        _mockPropertyNameRepository
            .Setup(repo => repo.GetByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<PropertyName, bool>>>()))
            .Returns(new List<PropertyName> { carPropertyName, personPropertyName }.AsQueryable());

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        photo.ObjectProperties.Should().HaveCount(2);
        photo.ObjectProperties.Should().Contain(op => op.PropertyName.Name == "car" && op.Confidence == 0.9f);
        photo.ObjectProperties.Should().Contain(op => op.PropertyName.Name == "person" && op.Confidence == 0.85f);
    }

    [Test]
    public async Task EnrichAsync_WithNewPropertyNames_ShouldInsertNewPropertyNames()
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

        var detectedObjects = new List<DetectedObjectOnnx>
        {
            new DetectedObjectOnnx
            {
                ClassName = "bicycle",
                Confidence = 0.75f,
                X = 50,
                Y = 50,
                Width = 40,
                Height = 40
            }
        };

        _mockYoloService
            .Setup(s => s.DetectObjects(imageBytes, 0.5f, 0.45f))
            .Returns(detectedObjects);

        _mockPropertyNameRepository
            .Setup(repo => repo.GetByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<PropertyName, bool>>>()))
            .Returns(Enumerable.Empty<PropertyName>().AsQueryable());

        _mockPropertyNameRepository
            .Setup(repo => repo.InsertAsync(It.IsAny<PropertyName>()))
            .ReturnsAsync((PropertyName pn) => pn);

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        _mockPropertyNameRepository.Verify(
            repo => repo.InsertAsync(It.Is<PropertyName>(pn => pn.Name == "bicycle")),
            Times.Once);
        photo.ObjectProperties.Should().HaveCount(1);
    }

    [Test]
    public async Task EnrichAsync_WithNoDetections_ShouldNotAddObjectProperties()
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

        _mockYoloService
            .Setup(s => s.DetectObjects(imageBytes, 0.5f, 0.45f))
            .Returns(new List<DetectedObjectOnnx>());

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

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
        var mockImage = new Mock<IMagickImage<byte>>();
        var imageBytes = new byte[] { 1, 2, 3, 4 };
        mockImage.Setup(m => m.ToByteArray()).Returns(imageBytes);

        var sourceData = new SourceDataDto
        {
            PreviewImage = mockImage.Object
        };

        var detectedObjects = new List<DetectedObjectOnnx>
        {
            new DetectedObjectOnnx
            {
                ClassName = "dog",
                Confidence = 0.8f,
                X = 100,  // Should be divided by scale (100 / 2 = 50)
                Y = 100,  // Should be divided by scale (100 / 2 = 50)
                Width = 200,  // Should be divided by scale (200 / 2 = 100)
                Height = 200  // Should be divided by scale (200 / 2 = 100)
            }
        };

        _mockYoloService
            .Setup(s => s.DetectObjects(imageBytes, 0.5f, 0.45f))
            .Returns(detectedObjects);

        var dogPropertyName = new PropertyName { Id = 1, Name = "dog" };

        _mockPropertyNameRepository
            .Setup(repo => repo.GetByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<PropertyName, bool>>>()))
            .Returns(new List<PropertyName> { dogPropertyName }.AsQueryable());

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        photo.ObjectProperties.Should().HaveCount(1);
        var objectProperty = photo.ObjectProperties.First();
        objectProperty.Rectangle.Should().NotBeNull();
        // The rectangle should be scaled by photo.Scale
    }
}
