using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Enrichers.Services;
using PhotoBank.Services.Models;
using PhotoBank.Services;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using ImageMagick;

namespace PhotoBank.UnitTests.Enrichers;

[TestFixture]
public class FaceEnricherTests
{
    private Mock<IFaceService> _mockFaceService;
    private Mock<IFacePreviewService> _mockFacePreviewService;
    private FaceEnricher _faceEnricher;

    [SetUp]
    public void Setup()
    {
        _mockFaceService = new Mock<IFaceService>();
        _mockFacePreviewService = new Mock<IFacePreviewService>();
        _faceEnricher = new FaceEnricher(_mockFaceService.Object, _mockFacePreviewService.Object);
    }

    [Test]
    public void EnricherType_ShouldReturnFace()
    {
        _faceEnricher.EnricherType.Should().Be(EnricherType.Face);
    }

    [Test]
    public void Dependencies_ShouldContainPreviewAndMetadata()
    {
        _faceEnricher.Dependencies.Should().Contain(new[] { typeof(PreviewEnricher), typeof(MetadataEnricher) });
    }

    [Test]
    public async Task EnrichAsync_NoFaces_NotDetected()
    {
        var photo = new Photo();
        var src = new SourceDataDto { PreviewImage = new MagickImage(MagickColors.Red, 10, 10) { Format = MagickFormat.Jpeg } };
        _mockFaceService.Setup(s => s.DetectFacesAsync(It.IsAny<byte[]>())).ReturnsAsync(new List<DetectedFace>());

        await _faceEnricher.EnrichAsync(photo, src);

        photo.FaceIdentifyStatus.Should().Be(FaceIdentifyStatus.NotDetected);
    }

    [Test]
    public async Task EnrichAsync_FacesDetected_AddsFaces()
    {
        var photo = new Photo();
        var src = new SourceDataDto { PreviewImage = new MagickImage(MagickColors.Red, 10, 10) { Format = MagickFormat.Jpeg } };
        var detected = new List<DetectedFace> { new() { FaceId = System.Guid.NewGuid(), FaceRectangle = new FaceRectangle { Height = 10, Width = 10, Top = 0, Left = 0 } } };
        _mockFaceService.Setup(s => s.DetectFacesAsync(It.IsAny<byte[]>())).ReturnsAsync(detected);
        _mockFacePreviewService.Setup(s => s.CreateFacePreview(It.IsAny<DetectedFace>(), It.IsAny<IMagickImage<byte>>(), It.IsAny<double>())).ReturnsAsync(new byte[] {1});

        await _faceEnricher.EnrichAsync(photo, src);

        photo.FaceIdentifyStatus.Should().Be(FaceIdentifyStatus.Detected);
        photo.Faces.Should().HaveCount(1);
        src.FaceImages.Should().HaveCount(1);
    }
}
