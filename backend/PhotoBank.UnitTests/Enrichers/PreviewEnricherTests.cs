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
    public async Task EnrichAsync_WithNullPhoto_ThrowsArgumentNullException()
    {
        // Arrange
        var sourceData = new SourceDataDto { AbsolutePath = _tempImagePath };

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _enricher.EnrichAsync(null, sourceData));
    }

    [Test]
    public async Task EnrichAsync_WithNullSource_ThrowsArgumentNullException()
    {
        // Arrange
        var photo = new Photo();

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _enricher.EnrichAsync(photo, null));
    }

    [Test]
    public async Task EnrichAsync_SetsPhotoProperties()
    {
        // Arrange
        var photo = new Photo();
        var sourceData = new SourceDataDto { AbsolutePath = _tempImagePath };
        var expectedScale = 0.5;

        _mockImageService
            .Setup(s => s.ResizeImage(It.IsAny<IMagickImage<byte>>(), out It.Ref<double>.IsAny))
            .Callback(new ResizeImageCallback((IMagickImage<byte> img, out double scale) =>
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
            .Setup(s => s.ResizeImage(It.IsAny<IMagickImage<byte>>(), out It.Ref<double>.IsAny))
            .Callback(new ResizeImageCallback((IMagickImage<byte> img, out double scale) =>
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
            .Setup(s => s.ResizeImage(It.IsAny<IMagickImage<byte>>(), out It.Ref<double>.IsAny))
            .Callback(new ResizeImageCallback((IMagickImage<byte> img, out double scale) =>
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
            .Setup(s => s.ResizeImage(It.IsAny<IMagickImage<byte>>(), out It.Ref<double>.IsAny))
            .Callback(new ResizeImageCallback((IMagickImage<byte> img, out double scale) =>
            {
                scale = 1.0;
            }));

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        photo.ImageHash.Should().NotBeNullOrEmpty();
        // Verify the hash is in the expected format (PerceptualHash ToString format)
        photo.ImageHash.Should().Contain(",");
    }

    [Test]
    public async Task EnrichAsync_CallsResizeImage()
    {
        // Arrange
        var photo = new Photo();
        var sourceData = new SourceDataDto { AbsolutePath = _tempImagePath };

        _mockImageService
            .Setup(s => s.ResizeImage(It.IsAny<IMagickImage<byte>>(), out It.Ref<double>.IsAny))
            .Callback(new ResizeImageCallback((IMagickImage<byte> img, out double scale) =>
            {
                scale = 1.0;
            }));

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        _mockImageService.Verify(
            s => s.ResizeImage(It.IsAny<IMagickImage<byte>>(), out It.Ref<double>.IsAny),
            Times.Once);
    }

    // Delegate for mocking out parameter
    private delegate void ResizeImageCallback(IMagickImage<byte> image, out double scale);
}
