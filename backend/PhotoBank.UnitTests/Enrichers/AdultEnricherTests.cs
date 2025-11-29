using System;
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
using PhotoBank.Services.Enrichers.Onnx;
using PhotoBank.Services.Models;

namespace PhotoBank.UnitTests.Enrichers;

[TestFixture]
public class AdultEnricherTests
{
    private Mock<INudeNetDetector> _mockDetector;
    private Mock<ILogger<AdultEnricher>> _mockLogger;
    private AdultEnricher _enricher;

    [SetUp]
    public void Setup()
    {
        _mockDetector = new Mock<INudeNetDetector>();
        _mockLogger = new Mock<ILogger<AdultEnricher>>();

        _enricher = new AdultEnricher(_mockDetector.Object, _mockLogger.Object);
    }

    [Test]
    public void EnricherType_ShouldReturnAdult()
    {
        // Act & Assert
        _enricher.EnricherType.Should().Be(EnricherType.Adult);
    }

    [Test]
    public void Dependencies_ShouldContainPreview()
    {
        // Act & Assert
        _enricher.Dependencies.Should().ContainSingle()
            .And.Contain(typeof(PreviewEnricher));
    }

    [Test]
    public async Task EnrichAsync_WithValidImageData_UpdatesPhotoProperties()
    {
        // Arrange
        var photo = new Photo { Id = 123 };
        var sourceData = new SourceDataDto
        {
            OriginalImage = new MagickImage(MagickColors.Red, 800, 600) { Format = MagickFormat.Jpeg },
            LetterboxedImage640 = new MagickImage(MagickColors.Red, 640, 640) { Format = MagickFormat.Jpeg },
            LetterboxScale = 0.8f,
            LetterboxPadX = 0,
            LetterboxPadY = 64
        };

        var detectionResult = new NudeNetDetectionResult
        {
            IsNsfw = true,
            NsfwConfidence = 0.872f, // High explicit content confidence
            IsRacy = false,
            RacyConfidence = 0.098f,
            Detections = new List<PhotoBank.Services.Onnx.Models.DetectedObjectOnnx>(),
            DetectionCounts = new Dictionary<string, int>
            {
                { "FEMALE_BREAST_EXPOSED", 2 }
            }
        };

        _mockDetector
            .Setup(d => d.Detect(
                It.IsAny<IMagickImage<byte>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<float>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Returns(detectionResult);

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        photo.IsAdultContent.Should().BeTrue();
        photo.AdultScore.Should().BeApproximately(0.872, 1e-6);
        photo.IsRacyContent.Should().BeFalse();
        photo.RacyScore.Should().BeApproximately(0.098, 1e-6);

        _mockDetector.Verify(d => d.Detect(
            It.IsAny<IMagickImage<byte>>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<float>(),
            It.IsAny<int>(),
            It.IsAny<int>()), Times.Once);
    }

    [Test]
    public async Task EnrichAsync_WithRacyContent_UpdatesPhotoProperties()
    {
        // Arrange
        var photo = new Photo { Id = 456 };
        var sourceData = new SourceDataDto
        {
            OriginalImage = new MagickImage(MagickColors.Blue, 1000, 800) { Format = MagickFormat.Jpeg },
            LetterboxedImage640 = new MagickImage(MagickColors.Blue, 640, 640) { Format = MagickFormat.Jpeg },
            LetterboxScale = 0.64f,
            LetterboxPadX = 0,
            LetterboxPadY = 51
        };

        var detectionResult = new NudeNetDetectionResult
        {
            IsNsfw = false,
            NsfwConfidence = 0.35f,
            IsRacy = true,
            RacyConfidence = 0.65f,
            Detections = new List<PhotoBank.Services.Onnx.Models.DetectedObjectOnnx>(),
            DetectionCounts = new Dictionary<string, int>
            {
                { "BUTTOCKS_EXPOSED", 1 },
                { "BELLY_EXPOSED", 1 }
            }
        };

        _mockDetector
            .Setup(d => d.Detect(
                It.IsAny<IMagickImage<byte>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<float>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Returns(detectionResult);

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        photo.IsAdultContent.Should().BeFalse();
        photo.AdultScore.Should().BeApproximately(0.35, 1e-5);
        photo.IsRacyContent.Should().BeTrue();
        photo.RacyScore.Should().BeApproximately(0.65, 1e-5);
    }

    [Test]
    public async Task EnrichAsync_WithSafeContent_UpdatesPhotoProperties()
    {
        // Arrange
        var photo = new Photo { Id = 789 };
        var sourceData = new SourceDataDto
        {
            OriginalImage = new MagickImage(MagickColors.Green, 640, 640) { Format = MagickFormat.Jpeg },
            LetterboxedImage640 = new MagickImage(MagickColors.Green, 640, 640) { Format = MagickFormat.Jpeg },
            LetterboxScale = 1.0f,
            LetterboxPadX = 0,
            LetterboxPadY = 0
        };

        var detectionResult = new NudeNetDetectionResult
        {
            IsNsfw = false,
            NsfwConfidence = 0.032f,
            IsRacy = false,
            RacyConfidence = 0.04f,
            Detections = new List<PhotoBank.Services.Onnx.Models.DetectedObjectOnnx>(),
            DetectionCounts = new Dictionary<string, int>
            {
                { "FACE_FEMALE", 1 },
                { "FACE_MALE", 1 }
            }
        };

        _mockDetector
            .Setup(d => d.Detect(
                It.IsAny<IMagickImage<byte>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<float>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Returns(detectionResult);

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        photo.IsAdultContent.Should().BeFalse();
        photo.AdultScore.Should().BeApproximately(0.032, 1e-6);
        photo.IsRacyContent.Should().BeFalse();
        photo.RacyScore.Should().BeApproximately(0.04, 1e-6); // Low racy score for safe content
    }

    [Test]
    public async Task EnrichAsync_WithNullLetterboxedImage_LogsWarningAndReturns()
    {
        // Arrange
        var photo = new Photo { Id = 123 };
        var sourceData = new SourceDataDto { LetterboxedImage640 = null };

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        _mockDetector.Verify(d => d.Detect(
            It.IsAny<IMagickImage<byte>>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<float>(),
            It.IsAny<int>(),
            It.IsAny<int>()), Times.Never);

        // Verify that a warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No letterboxed image available")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task EnrichAsync_WithNullOriginalImage_LogsWarningAndReturns()
    {
        // Arrange
        var photo = new Photo { Id = 123 };
        var sourceData = new SourceDataDto
        {
            LetterboxedImage640 = new MagickImage(MagickColors.Red, 640, 640),
            OriginalImage = null
        };

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        _mockDetector.Verify(d => d.Detect(
            It.IsAny<IMagickImage<byte>>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<float>(),
            It.IsAny<int>(),
            It.IsAny<int>()), Times.Never);

        // Verify that a warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No original image available")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task EnrichAsync_WithNullSourceData_LogsWarningAndReturns()
    {
        // Arrange
        var photo = new Photo { Id = 123 };

        // Act
        await _enricher.EnrichAsync(photo, null);

        // Assert
        _mockDetector.Verify(d => d.Detect(
            It.IsAny<IMagickImage<byte>>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<float>(),
            It.IsAny<int>(),
            It.IsAny<int>()), Times.Never);
    }

    [Test]
    public void EnrichAsync_DetectorThrowsException_PropagatesException()
    {
        // Arrange
        var photo = new Photo { Id = 123 };
        var sourceData = new SourceDataDto
        {
            OriginalImage = new MagickImage(MagickColors.Red, 800, 600) { Format = MagickFormat.Jpeg },
            LetterboxedImage640 = new MagickImage(MagickColors.Red, 640, 640) { Format = MagickFormat.Jpeg },
            LetterboxScale = 0.8f,
            LetterboxPadX = 0,
            LetterboxPadY = 64
        };

        _mockDetector
            .Setup(d => d.Detect(
                It.IsAny<IMagickImage<byte>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<float>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Throws(new InvalidOperationException("Model inference failed"));

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _enricher.EnrichAsync(photo, sourceData));

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error during NudeNet detection")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task EnrichAsync_CancellationRequested_PropagatesCancellation()
    {
        // Arrange
        var photo = new Photo { Id = 123 };
        var sourceData = new SourceDataDto
        {
            OriginalImage = new MagickImage(MagickColors.Red, 800, 600) { Format = MagickFormat.Jpeg },
            LetterboxedImage640 = new MagickImage(MagickColors.Red, 640, 640) { Format = MagickFormat.Jpeg },
            LetterboxScale = 0.8f,
            LetterboxPadX = 0,
            LetterboxPadY = 64
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockDetector
            .Setup(d => d.Detect(
                It.IsAny<IMagickImage<byte>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<float>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Throws(new TaskCanceledException());

        // Act & Assert
        Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await _enricher.EnrichAsync(photo, sourceData, cts.Token));
    }
}
