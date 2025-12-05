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
using PhotoBank.Services.FaceRecognition;
using PhotoBank.Services.FaceRecognition.Abstractions;
using PhotoBank.Services.Models;

namespace PhotoBank.UnitTests.Enrichers;

[TestFixture]
public class UnifiedFaceEnricherTests
{
    private Mock<IUnifiedFaceService> _mockFaceService;
    private Mock<ILogger<UnifiedFaceEnricher>> _mockLogger;
    private UnifiedFaceEnricher _enricher;

    [SetUp]
    public void Setup()
    {
        _mockFaceService = new Mock<IUnifiedFaceService>();
        _mockLogger = new Mock<ILogger<UnifiedFaceEnricher>>();

        _enricher = new UnifiedFaceEnricher(
            _mockFaceService.Object,
 _mockLogger.Object);
    }

    [Test]
    public void EnricherType_ShouldReturnFace()
    {
        // Act & Assert
        _enricher.EnricherType.Should().Be(EnricherType.Face);
    }

    [Test]
    public void Dependencies_ShouldContainDuplicate()
    {
        // Act & Assert
        _enricher.Dependencies.Should().ContainSingle()
            .And.Contain(typeof(DuplicateEnricher));
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
            new DetectedFaceDto("face1", 0.95f, 25f, "Female", new FaceBoundingBox(0.1f, 0.1f, 0.2f, 0.2f)),
            new DetectedFaceDto("face2", 0.92f, 35f, "Male", new FaceBoundingBox(0.5f, 0.5f, 0.2f, 0.2f))
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
        photo.Faces[0].Rectangle.Should().NotBeNull(); // Rectangle should be set from BoundingBox

        photo.Faces[1].PhotoId.Should().Be(123);
        photo.Faces[1].Age.Should().Be(35f);
        photo.Faces[1].Gender.Should().BeTrue(); // Male
        photo.Faces[1].IdentityStatus.Should().Be(IdentityStatus.NotIdentified);
        photo.Faces[1].Rectangle.Should().NotBeNull(); // Rectangle should be set from BoundingBox
    }

    [Test]
    public async Task EnrichAsync_FaceWithBoundingBox_CreatesRectangleGeometry()
    {
        // Arrange
        var photo = new Photo { Id = 123, Scale = 1.0 };
        var sourceData = new SourceDataDto
        {
            PreviewImage = new MagickImage(MagickColors.Red, 100, 100) { Format = MagickFormat.Jpeg }
        };

        var detectedFaces = new List<DetectedFaceDto>
        {
            new DetectedFaceDto("face1", 0.95f, 25f, "Female", new FaceBoundingBox(0.1f, 0.2f, 0.3f, 0.4f))
        };

        _mockFaceService
            .Setup(s => s.DetectFacesAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(detectedFaces);

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        photo.Faces.Should().HaveCount(1);
        var face = photo.Faces[0];
        face.Rectangle.Should().NotBeNull();
        face.Rectangle!.Coordinates.Should().HaveCount(5); // Rectangle has 5 coordinates (closed polygon)

        // Verify the Rectangle coordinates match the bounding box
        // BoundingBox(0.1, 0.2, 0.3, 0.4) on 100x100 image = (10, 20, 30, 40)
        face.Rectangle.Coordinates[0].X.Should().Be(10);
        face.Rectangle.Coordinates[0].Y.Should().Be(20);
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
            new DetectedFaceDto("face1", 0.95f, 25f, null, new FaceBoundingBox(0.1f, 0.1f, 0.2f, 0.2f))
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
    public async Task EnrichAsync_DetectionThrowsException_PropagatesException()
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
        Assert.ThrowsAsync<System.Exception>(async () =>
            await _enricher.EnrichAsync(photo, sourceData));
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
        Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await _enricher.EnrichAsync(photo, sourceData, cts.Token));
    }

    [Test]
    public async Task EnrichAsync_FaceWithIdentification_SetsIdentifiedStatusAndPersonId()
    {
        // Arrange
        var photo = new Photo { Id = 123 };
        var sourceData = new SourceDataDto
        {
            PreviewImage = new MagickImage(MagickColors.Red, 100, 100) { Format = MagickFormat.Jpeg }
        };

        var detectedFaces = new List<DetectedFaceDto>
        {
            new DetectedFaceDto(
                ProviderFaceId: "face1",
                Confidence: 0.95f,
                Age: 30f,
                Gender: "Male",
                BoundingBox: new FaceBoundingBox(0.1f, 0.1f, 0.2f, 0.2f),
                Emotion: null,
                EmotionScores: null,
                IdentifiedPersonId: 42,
                IdentificationConfidence: 0.87f)
        };

        _mockFaceService
            .Setup(s => s.DetectFacesAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(detectedFaces);

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        photo.FaceIdentifyStatus.Should().Be(FaceIdentifyStatus.Detected);
        photo.Faces.Should().HaveCount(1);

        var face = photo.Faces[0];
        face.IdentityStatus.Should().Be(IdentityStatus.Identified);
        face.PersonId.Should().Be(42);
        face.IdentifiedWithConfidence.Should().BeApproximately(0.87, 0.0001);
    }

    [Test]
    public async Task EnrichAsync_MixedIdentificationResults_SetsCorrectStatusForEachFace()
    {
        // Arrange
        var photo = new Photo { Id = 123 };
        var sourceData = new SourceDataDto
        {
            PreviewImage = new MagickImage(MagickColors.Red, 100, 100) { Format = MagickFormat.Jpeg }
        };

        var detectedFaces = new List<DetectedFaceDto>
        {
            // Identified face
            new DetectedFaceDto(
                ProviderFaceId: "face1",
                Confidence: 0.95f,
                Age: 30f,
                Gender: "Male",
                BoundingBox: new FaceBoundingBox(0.1f, 0.1f, 0.2f, 0.2f),
                Emotion: null,
                EmotionScores: null,
                IdentifiedPersonId: 42,
                IdentificationConfidence: 0.87f),
            // Not identified face
            new DetectedFaceDto(
                ProviderFaceId: "face2",
                Confidence: 0.92f,
                Age: 25f,
                Gender: "Female",
                BoundingBox: new FaceBoundingBox(0.5f, 0.5f, 0.2f, 0.2f),
                Emotion: null,
                EmotionScores: null,
                IdentifiedPersonId: null,
                IdentificationConfidence: null)
        };

        _mockFaceService
            .Setup(s => s.DetectFacesAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(detectedFaces);

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        photo.FaceIdentifyStatus.Should().Be(FaceIdentifyStatus.Detected);
        photo.Faces.Should().HaveCount(2);

        // First face - identified
        photo.Faces[0].IdentityStatus.Should().Be(IdentityStatus.Identified);
        photo.Faces[0].PersonId.Should().Be(42);
        photo.Faces[0].IdentifiedWithConfidence.Should().BeApproximately(0.87, 0.0001);

        // Second face - not identified
        photo.Faces[1].IdentityStatus.Should().Be(IdentityStatus.NotIdentified);
        photo.Faces[1].PersonId.Should().BeNull();
        photo.Faces[1].IdentifiedWithConfidence.Should().BeApproximately(0.0, 0.0001);
    }

    [Test]
    public async Task EnrichAsync_FaceWithoutIdentificationConfidence_SetsConfidenceToZero()
    {
        // Arrange
        var photo = new Photo { Id = 123 };
        var sourceData = new SourceDataDto
        {
            PreviewImage = new MagickImage(MagickColors.Red, 100, 100) { Format = MagickFormat.Jpeg }
        };

        var detectedFaces = new List<DetectedFaceDto>
        {
            new DetectedFaceDto(
                ProviderFaceId: "face1",
                Confidence: 0.95f,
                Age: 30f,
                Gender: "Male",
                BoundingBox: new FaceBoundingBox(0.1f, 0.1f, 0.2f, 0.2f),
                Emotion: null,
                EmotionScores: null,
                IdentifiedPersonId: null,
                IdentificationConfidence: null)
        };

        _mockFaceService
            .Setup(s => s.DetectFacesAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(detectedFaces);

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        photo.Faces.Should().HaveCount(1);
        photo.Faces[0].IdentityStatus.Should().Be(IdentityStatus.NotIdentified);
        photo.Faces[0].PersonId.Should().BeNull();
        photo.Faces[0].IdentifiedWithConfidence.Should().BeApproximately(0.0, 0.0001);
    }
}
