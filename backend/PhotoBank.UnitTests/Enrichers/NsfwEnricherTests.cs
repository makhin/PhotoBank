using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Enrichers.Onnx;
using PhotoBank.Services.Models;

namespace PhotoBank.UnitTests.Enrichers;

[TestFixture]
public class NsfwEnricherTests
{
    private Mock<INsfwDetector> _mockDetector;
    private Mock<ILogger<NsfwEnricher>> _mockLogger;
    private NsfwEnricher _enricher;

    [SetUp]
    public void Setup()
    {
        _mockDetector = new Mock<INsfwDetector>();
        _mockLogger = new Mock<ILogger<NsfwEnricher>>();

        _enricher = new NsfwEnricher(_mockDetector.Object, _mockLogger.Object);
    }

    [Test]
    public void EnricherType_ShouldReturnNsfw()
    {
        // Act & Assert
        _enricher.EnricherType.Should().Be(EnricherType.Nsfw);
    }

    [Test]
    public void Dependencies_ShouldContainMetadata()
    {
        // Act & Assert
        _enricher.Dependencies.Should().ContainSingle()
            .And.Contain(typeof(MetadataEnricher));
    }

    [Test]
    public async Task EnrichAsync_WithValidImageData_UpdatesPhotoProperties()
    {
        // Arrange
        var photo = new Photo { Id = 123 };
        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG header
        var sourceData = new SourceDataDto { Bytes = imageBytes };

        var detectionResult = new NsfwDetectionResult
        {
            IsNsfw = true,
            NsfwConfidence = 0.872f, // Raw porn score (highest NSFW indicator)
            IsRacy = false,
            RacyConfidence = 0.098f, // Raw sexy score
            Scores = new Dictionary<string, float>
            {
                { "porn", 0.872f },
                { "sexy", 0.098f },
                { "hentai", 0.012f },
                { "neutral", 0.015f },
                { "drawings", 0.003f }
            }
        };

        _mockDetector
            .Setup(d => d.Detect(It.IsAny<byte[]>()))
            .Returns(detectionResult);

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        photo.IsAdultContent.Should().BeTrue();
        photo.AdultScore.Should().Be(0.872);
        photo.IsRacyContent.Should().BeFalse();
        photo.RacyScore.Should().Be(0.098);

        _mockDetector.Verify(d => d.Detect(imageBytes), Times.Once);
    }

    [Test]
    public async Task EnrichAsync_WithRacyContent_UpdatesPhotoProperties()
    {
        // Arrange
        var photo = new Photo { Id = 456 };
        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var sourceData = new SourceDataDto { Bytes = imageBytes };

        var detectionResult = new NsfwDetectionResult
        {
            IsNsfw = false,
            NsfwConfidence = 0.35f,
            IsRacy = true,
            RacyConfidence = 0.65f,
            Scores = new Dictionary<string, float>
            {
                { "porn", 0.05f },
                { "sexy", 0.65f },
                { "hentai", 0.02f },
                { "neutral", 0.25f },
                { "drawings", 0.03f }
            }
        };

        _mockDetector
            .Setup(d => d.Detect(It.IsAny<byte[]>()))
            .Returns(detectionResult);

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        photo.IsAdultContent.Should().BeFalse();
        photo.AdultScore.Should().Be(0.35);
        photo.IsRacyContent.Should().BeTrue();
        photo.RacyScore.Should().Be(0.65);
    }

    [Test]
    public async Task EnrichAsync_WithSafeContent_UpdatesPhotoProperties()
    {
        // Arrange
        var photo = new Photo { Id = 789 };
        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var sourceData = new SourceDataDto { Bytes = imageBytes };

        var detectionResult = new NsfwDetectionResult
        {
            IsNsfw = false,
            NsfwConfidence = 0.032f, // Max(porn=0.01, sexy*0.8=0.032, hentai=0.01)
            IsRacy = false,
            RacyConfidence = 0.04f, // Raw sexy score
            Scores = new Dictionary<string, float>
            {
                { "porn", 0.01f },
                { "sexy", 0.04f },
                { "hentai", 0.01f },
                { "neutral", 0.92f },
                { "drawings", 0.02f }
            }
        };

        _mockDetector
            .Setup(d => d.Detect(It.IsAny<byte[]>()))
            .Returns(detectionResult);

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        photo.IsAdultContent.Should().BeFalse();
        photo.AdultScore.Should().Be(0.032);
        photo.IsRacyContent.Should().BeFalse();
        photo.RacyScore.Should().Be(0.04); // Low racy score for safe content
    }

    [Test]
    public async Task EnrichAsync_WithNullImageData_LogsWarningAndReturns()
    {
        // Arrange
        var photo = new Photo { Id = 123 };
        var sourceData = new SourceDataDto { Bytes = null };

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        _mockDetector.Verify(d => d.Detect(It.IsAny<byte[]>()), Times.Never);

        // Verify that a warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No image data available")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task EnrichAsync_WithEmptyImageData_LogsWarningAndReturns()
    {
        // Arrange
        var photo = new Photo { Id = 123 };
        var sourceData = new SourceDataDto { Bytes = Array.Empty<byte>() };

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        _mockDetector.Verify(d => d.Detect(It.IsAny<byte[]>()), Times.Never);
    }

    [Test]
    public async Task EnrichAsync_WithNullSourceData_LogsWarningAndReturns()
    {
        // Arrange
        var photo = new Photo { Id = 123 };

        // Act
        await _enricher.EnrichAsync(photo, null);

        // Assert
        _mockDetector.Verify(d => d.Detect(It.IsAny<byte[]>()), Times.Never);
    }

    [Test]
    public void EnrichAsync_DetectorThrowsException_PropagatesException()
    {
        // Arrange
        var photo = new Photo { Id = 123 };
        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var sourceData = new SourceDataDto { Bytes = imageBytes };

        _mockDetector
            .Setup(d => d.Detect(It.IsAny<byte[]>()))
            .Throws(new InvalidOperationException("Model inference failed"));

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _enricher.EnrichAsync(photo, sourceData));

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error during NSFW detection")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task EnrichAsync_CancellationRequested_PropagatesCancellation()
    {
        // Arrange
        var photo = new Photo { Id = 123 };
        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var sourceData = new SourceDataDto { Bytes = imageBytes };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockDetector
            .Setup(d => d.Detect(It.IsAny<byte[]>()))
            .Throws(new TaskCanceledException());

        // Act & Assert
        Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await _enricher.EnrichAsync(photo, sourceData, cts.Token));
    }
}
