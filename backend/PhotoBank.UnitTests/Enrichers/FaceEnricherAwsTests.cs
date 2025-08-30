using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Rekognition.Model;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Models;
using ImageMagick;
using PhotoBank.Services;

namespace PhotoBank.UnitTests.Enrichers;

[TestFixture]
public class FaceEnricherAwsTests
{
    private Mock<IFaceServiceAws> _mockFaceService;
    private FaceEnricherAws _faceEnricher;

    [SetUp]
    public void Setup()
    {
        _mockFaceService = new Mock<IFaceServiceAws>();
        _faceEnricher = new FaceEnricherAws(_mockFaceService.Object);
    }

    [Test]
    public async Task EnrichAsync_NoFaces_NotDetected()
    {
        var photo = new Photo();
        var src = new SourceDataDto { PreviewImage = new MagickImage(MagickColors.Red, 10, 10) { Format = MagickFormat.Jpeg } };
        _mockFaceService.Setup(s => s.DetectFacesAsync(It.IsAny<byte[]>())).ReturnsAsync(new List<FaceDetail>());

        await _faceEnricher.EnrichAsync(photo, src);

        photo.FaceIdentifyStatus.Should().Be(FaceIdentifyStatus.NotDetected);
    }

    [Test]
    public async Task EnrichAsync_FacesDetected_AddsFaces()
    {
        var photo = new Photo();
        var src = new SourceDataDto { PreviewImage = new MagickImage(MagickColors.Red, 100, 100) { Format = MagickFormat.Jpeg }, OriginalImage = new MagickImage(MagickColors.Red, 100, 100) { Format = MagickFormat.Jpeg } };
        var detected = new List<FaceDetail> { new() { BoundingBox = new BoundingBox { Height = 0.5f, Width = 0.5f, Top = 0.1f, Left = 0.1f }, AgeRange = new AgeRange { High = 30, Low = 20 }, Gender = new Gender { Value = "Male" }, Smile = new Smile { Confidence = 0.5f } } };
        _mockFaceService.Setup(s => s.DetectFacesAsync(It.IsAny<byte[]>())).ReturnsAsync(detected);

        await _faceEnricher.EnrichAsync(photo, src);

        photo.FaceIdentifyStatus.Should().Be(FaceIdentifyStatus.Detected);
        photo.Faces.Should().HaveCount(1);
        src.FaceImages.Should().HaveCount(1);
    }
}
