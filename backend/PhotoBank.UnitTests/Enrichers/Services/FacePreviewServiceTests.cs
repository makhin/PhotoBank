using System;
using System.Threading.Tasks;
using FluentAssertions;
using Minio;
using Minio.DataModel.Args;
using Minio.DataModel;
using Minio.DataModel.Response;
using Moq;
using NUnit.Framework;
using PhotoBank.Services.Enrichers.Services;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using ImageMagick;

namespace PhotoBank.UnitTests.Enrichers.Services
{
    [TestFixture]
    public class FacePreviewServiceTests
    {
        [Test]
        public async Task CreateFacePreview_UploadsToS3()
        {
            var minio = new Mock<IMinioClient>();
            var stat = (ObjectStat)Activator.CreateInstance(typeof(ObjectStat), nonPublic: true);
            minio.Setup(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), default)).ReturnsAsync((PutObjectResponse?)null).Verifiable();
            minio.Setup(m => m.StatObjectAsync(It.IsAny<StatObjectArgs>(), default)).ReturnsAsync(stat);
            var service = new FacePreviewService(minio.Object);
            var image = new MagickImage(MagickColors.Red, 10, 10) { Format = MagickFormat.Jpeg };
            var face = new DetectedFace { FaceRectangle = new FaceRectangle { Height = 10, Width = 10, Top = 0, Left = 0 } };

            var (key, etag) = await service.CreateFacePreview(face, image, 1);

            minio.Verify(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), default), Times.Once);
            key.Should().StartWith("faces/");
            etag.Should().BeEmpty();
        }
    }
}
