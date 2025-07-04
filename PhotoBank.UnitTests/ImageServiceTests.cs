using FluentAssertions;
using ImageMagick;
using NUnit.Framework;
using PhotoBank.Services;

namespace PhotoBank.UnitTests;

[TestFixture]
public class ImageServiceTests
{
    private IImageService _service = null!;

    [SetUp]
    public void Setup()
    {
        _service = new ImageService();
    }

    [Test]
    public void ResizeImage_LandscapeImage_ResizesToMaxWidth()
    {
        using var image = new MagickImage(MagickColors.Red, 4000, 1000);

        _service.ResizeImage(image, out var scale);

        image.Width.Should().Be(ImageService.MaxSize);
        image.Height.Should().Be(480);
        scale.Should().BeApproximately((double)ImageService.MaxSize / 4000, 0.0001);
    }

    [Test]
    public void ResizeImage_PortraitImage_ResizesToMaxHeight()
    {
        using var image = new MagickImage(MagickColors.Red, 1000, 3000);

        _service.ResizeImage(image, out var scale);

        image.Height.Should().Be(ImageService.MaxSize);
        image.Width.Should().Be(640);
        scale.Should().BeApproximately((double)ImageService.MaxSize / 3000, 0.0001);
    }

    [Test]
    public void ResizeImage_SmallImage_DoesNotChangeSize()
    {
        using var image = new MagickImage(MagickColors.Red, 800, 600);

        _service.ResizeImage(image, out var scale);

        image.Width.Should().Be(800);
        image.Height.Should().Be(600);
        scale.Should().Be(1);
    }
}
