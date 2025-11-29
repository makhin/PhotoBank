using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using ImageMagick;
using Moq;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Services;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Models;
using File = System.IO.File;

namespace PhotoBank.UnitTests.Enrichers;

[TestFixture]
public class PreviewEnricherTests
{
    private Mock<IImageService> _mockImageService;
    private PreviewEnricher _enricher;
    private string _tempImagePath;

    [SetUp]
    public void Setup()
    {
        _mockImageService = new Mock<IImageService>();
        _enricher = new PreviewEnricher(_mockImageService.Object);

        // Create a temporary test image
        _tempImagePath = Path.Combine(Path.GetTempPath(), $"test_image_{Guid.NewGuid()}.jpg");
        using var image = new MagickImage(MagickColors.Red, 100, 100);
        image.Format = MagickFormat.Jpeg;
        image.Write(_tempImagePath);
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up temporary test image
        if (File.Exists(_tempImagePath))
        {
            File.Delete(_tempImagePath);
        }
    }

    [Test]
    public void EnricherType_ShouldReturnPreview()
    {
        // Act & Assert
        _enricher.EnricherType.Should().Be(EnricherType.Preview);
    }

    [Test]
    public void Dependencies_ShouldBeEmpty()
    {
        // Act & Assert
        _enricher.Dependencies.Should().BeEmpty();
    }

    [Test]
    public void Constructor_WithNullImageService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PreviewEnricher(null));
    }

    [Test]
    public async Task EnrichAsync_SetsPhotoProperties()
    {
        // Arrange
        var photo = new Photo();
        var sourceData = new SourceDataDto { AbsolutePath = _tempImagePath };
        var expectedScale = 0.5;

        _mockImageService
            .Setup(s => s.ResizeImage(It.IsAny<MagickImage>(), out It.Ref<double>.IsAny))
            .Callback(new ResizeImageCallback((MagickImage img, out double scale) =>
            {
                scale = expectedScale;
            }));

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        photo.Height.Should().Be(100);
        photo.Width.Should().Be(100);
        photo.Orientation.Should().NotBeNull();
        photo.Scale.Should().Be(expectedScale);
        photo.ImageHash.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task EnrichAsync_SetsOriginalImage()
    {
        // Arrange
        var photo = new Photo();
        var sourceData = new SourceDataDto { AbsolutePath = _tempImagePath };

        _mockImageService
            .Setup(s => s.ResizeImage(It.IsAny<MagickImage>(), out It.Ref<double>.IsAny))
            .Callback(new ResizeImageCallback((MagickImage img, out double scale) =>
            {
                scale = 1.0;
            }));

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        sourceData.OriginalImage.Should().NotBeNull();
        sourceData.OriginalImage.Width.Should().Be(100);
        sourceData.OriginalImage.Height.Should().Be(100);
    }

    [Test]
    public async Task EnrichAsync_SetsPreviewImage()
    {
        // Arrange
        var photo = new Photo();
        var sourceData = new SourceDataDto { AbsolutePath = _tempImagePath };

        _mockImageService
            .Setup(s => s.ResizeImage(It.IsAny<MagickImage>(), out It.Ref<double>.IsAny))
            .Callback(new ResizeImageCallback((MagickImage img, out double scale) =>
            {
                scale = 1.0;
            }));

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        sourceData.PreviewImage.Should().NotBeNull();
        sourceData.PreviewImage.Format.Should().Be(MagickFormat.Jpg);
    }

    [Test]
    public async Task EnrichAsync_ComputesImageHash()
    {
        // Arrange
        var photo = new Photo();
        var sourceData = new SourceDataDto { AbsolutePath = _tempImagePath };

        _mockImageService
            .Setup(s => s.ResizeImage(It.IsAny<MagickImage>(), out It.Ref<double>.IsAny))
            .Callback(new ResizeImageCallback((MagickImage img, out double scale) =>
            {
                scale = 1.0;
            }));

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        photo.ImageHash.Should().NotBeNullOrEmpty();
        // Verify the hash is in hex format (current PerceptualHash ToString format)
        photo.ImageHash.Should().MatchRegex("^[0-9a-f]+$");
        photo.ImageHash.Length.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task EnrichAsync_CallsResizeImage()
    {
        // Arrange
        var photo = new Photo();
        var sourceData = new SourceDataDto { AbsolutePath = _tempImagePath };

        _mockImageService
            .Setup(s => s.ResizeImage(It.IsAny<MagickImage>(), out It.Ref<double>.IsAny))
            .Callback(new ResizeImageCallback((MagickImage img, out double scale) =>
            {
                scale = 1.0;
            }));

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        _mockImageService.Verify(
            s => s.ResizeImage(It.IsAny<MagickImage>(), out It.Ref<double>.IsAny),
            Times.Once);
    }

    [Test]
    public async Task EnrichAsync_CreatesLetterboxedImage640()
    {
        // Arrange
        var photo = new Photo();
        var sourceData = new SourceDataDto { AbsolutePath = _tempImagePath };

        _mockImageService
            .Setup(s => s.ResizeImage(It.IsAny<MagickImage>(), out It.Ref<double>.IsAny))
            .Callback(new ResizeImageCallback((MagickImage img, out double scale) =>
            {
                scale = 1.0;
            }));

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        sourceData.LetterboxedImage640.Should().NotBeNull();
        sourceData.LetterboxedImage640.Width.Should().Be(640);
        sourceData.LetterboxedImage640.Height.Should().Be(640);
    }

    [Test]
    public async Task EnrichAsync_SetsLetterboxParameters()
    {
        // Arrange
        var photo = new Photo();
        var sourceData = new SourceDataDto { AbsolutePath = _tempImagePath };

        _mockImageService
            .Setup(s => s.ResizeImage(It.IsAny<MagickImage>(), out It.Ref<double>.IsAny))
            .Callback(new ResizeImageCallback((MagickImage img, out double scale) =>
            {
                scale = 1.0;
            }));

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        sourceData.LetterboxScale.Should().BeGreaterThan(0);
        sourceData.LetterboxPadX.Should().BeGreaterThanOrEqualTo(0);
        sourceData.LetterboxPadY.Should().BeGreaterThanOrEqualTo(0);
    }

    // Delegate for mocking out parameter
    private delegate void ResizeImageCallback(MagickImage image, out double scale);
}
