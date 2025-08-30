using System.Threading.Tasks;
using FluentAssertions;
using ImageMagick;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using NUnit.Framework;
using PhotoBank.Services.Enrichers.Services;

namespace PhotoBank.UnitTests.Enrichers.Services;

[TestFixture]
public class FacePreviewServiceTests
{
    [Test]
    public async Task CreateFacePreview_ReturnsBytes()
    {
        var service = new FacePreviewService();
        var image = new MagickImage(MagickColors.Red, 10, 10) { Format = MagickFormat.Jpeg };
        var face = new DetectedFace { FaceRectangle = new FaceRectangle { Height = 10, Width = 10, Top = 0, Left = 0 } };

        var bytes = await service.CreateFacePreview(face, image, 1);

        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(0);
    }
}
