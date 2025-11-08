using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using ImageMagick;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Enrichers.Services;
using PhotoBank.Services.FaceRecognition;
using PhotoBank.Services.FaceRecognition.Abstractions;
using PhotoBank.Services.Models;

namespace PhotoBank.UnitTests.Enrichers;

[TestFixture]
public class UnifiedFaceEnricherTests
{
    private Mock<UnifiedFaceService> _mockFaceService;
    private Mock<IFacePreviewService> _mockFacePreviewService;
    private Mock<ILogger<UnifiedFaceEnricher>> _mockLogger;
    private UnifiedFaceEnricher _enricher;

    [SetUp]
    public void Setup()
    {
        _mockFaceService = new Mock<UnifiedFaceService>(
            Mock.Of<IFaceProvider>(),
            Mock.Of<Repositories.IRepository<Person>>(),
            Mock.Of<Repositories.IRepository<Face>>(),
            Mock.Of<IFaceStorageService>(),
            Mock.Of<ILogger<UnifiedFaceService>>());

        _mockFacePreviewService = new Mock<IFacePreviewService>();
        _mockLogger = new Mock<ILogger<UnifiedFaceEnricher>>();

        _enricher = new UnifiedFaceEnricher(
            _mockFaceService.Object,
            _mockFacePreviewService.Object,
            _mockLogger.Object);
    }

    [Test]
    public void EnricherType_ShouldReturnFace()
    {
        // Act & Assert
        _enricher.EnricherType.Should().Be(EnricherType.Face);
    }

    [Test]
    public void Dependencies_ShouldContainPreviewAndMetadata()
    {
        // Act & Assert
        _enricher.Dependencies.Should().Contain(new[] { typeof(PreviewEnricher), typeof(MetadataEnricher) });
    }

    [Test]
    public async Task EnrichAsync_NoPreviewImage_SetsNotDetectedStatus()
    {
        // Arrange
        var photo = new Photo();
        var sourceData = new SourceDataDto { PreviewImage = null };

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        photo.FaceIdentifyStatus.Should().Be(FaceIdentifyStatus.NotDetected);
        _mockFaceService.Verify(s => s.DetectFacesAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task EnrichAsync_NoFacesDetected_SetsNotDetectedStatus()
    {
        // Arrange
        var photo = new Photo();
        var sourceData = new SourceDataDto
        {
            PreviewImage = new MagickImage(MagickColors.Red, 100, 100) { Format = MagickFormat.Jpeg }
        };

        _mockFaceService
            .Setup(s => s.DetectFacesAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DetectedFaceDto>());

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        photo.FaceIdentifyStatus.Should().Be(FaceIdentifyStatus.NotDetected);
        photo.Faces.Should().BeNullOrEmpty();
    }

    [Test]
    public async Task EnrichAsync_FacesDetected_AddsFacesToPhoto()
    {
        // Arrange
        var photo = new Photo { Id = 123 };
        var sourceData = new SourceDataDto
        {
            PreviewImage = new MagickImage(MagickColors.Red, 100, 100) { Format = MagickFormat.Jpeg }
        };

        var detectedFaces = new List<DetectedFaceDto>
        {
            new DetectedFaceDto("face1", 0.95f, 25f, "Female"),
            new DetectedFaceDto("face2", 0.92f, 35f, "Male")
        };

        _mockFaceService
            .Setup(s => s.DetectFacesAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(detectedFaces);

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        photo.FaceIdentifyStatus.Should().Be(FaceIdentifyStatus.Detected);
        photo.Faces.Should().HaveCount(2);
        sourceData.FaceImages.Should().HaveCount(2);

        photo.Faces[0].PhotoId.Should().Be(123);
        photo.Faces[0].Age.Should().Be(25f);
        photo.Faces[0].Gender.Should().BeFalse(); // Female
        photo.Faces[0].IdentityStatus.Should().Be(IdentityStatus.NotIdentified);

        photo.Faces[1].PhotoId.Should().Be(123);
        photo.Faces[1].Age.Should().Be(35f);
        photo.Faces[1].Gender.Should().BeTrue(); // Male
        photo.Faces[1].IdentityStatus.Should().Be(IdentityStatus.NotIdentified);
    }

    [Test]
    public async Task EnrichAsync_FaceDetectedWithNoGender_SetsGenderToNull()
    {
        // Arrange
        var photo = new Photo { Id = 123 };
        var sourceData = new SourceDataDto
        {
            PreviewImage = new MagickImage(MagickColors.Red, 100, 100) { Format = MagickFormat.Jpeg }
        };

        var detectedFaces = new List<DetectedFaceDto>
        {
            new DetectedFaceDto("face1", 0.95f, 25f, null)
        };

        _mockFaceService
            .Setup(s => s.DetectFacesAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(detectedFaces);

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        photo.Faces.Should().HaveCount(1);
        photo.Faces[0].Gender.Should().BeNull();
    }

    [Test]
    public async Task EnrichAsync_DetectionThrowsException_SetsProcessingErrorStatus()
    {
        // Arrange
        var photo = new Photo();
        var sourceData = new SourceDataDto
        {
            PreviewImage = new MagickImage(MagickColors.Red, 100, 100) { Format = MagickFormat.Jpeg }
        };

        _mockFaceService
            .Setup(s => s.DetectFacesAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new System.Exception("API error"));

        // Act & Assert
        await Assert.ThrowsAsync<System.Exception>(async () =>
            await _enricher.EnrichAsync(photo, sourceData));

        photo.FaceIdentifyStatus.Should().Be(FaceIdentifyStatus.ProcessingError);
    }

    [Test]
    public async Task EnrichAsync_CancellationRequested_PropagatesCancellation()
    {
        // Arrange
        var photo = new Photo();
        var sourceData = new SourceDataDto
        {
            PreviewImage = new MagickImage(MagickColors.Red, 100, 100) { Format = MagickFormat.Jpeg }
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockFaceService
            .Setup(s => s.DetectFacesAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await _enricher.EnrichAsync(photo, sourceData, cts.Token));
    }
}
