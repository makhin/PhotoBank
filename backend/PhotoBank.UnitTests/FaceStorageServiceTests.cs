using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Minio;
using Minio.DataModel.Args;
using Minio.DataModel;
using Moq;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Services;

namespace PhotoBank.UnitTests;

[TestFixture]
public class FaceStorageServiceTests
{
    [Test]
    public async Task OpenReadStreamAsync_UsesS3_WhenImageMissing()
    {
        var minio = new Mock<IMinioClient>();
        minio.Setup(m => m.GetObjectAsync(It.IsAny<GetObjectArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ObjectStat)Activator.CreateInstance(typeof(ObjectStat), nonPublic: true)!)
            .Verifiable();

        var service = new FaceStorageService(minio.Object);
        var face = new Face { Id = 1, S3Key_Image = "face1" };

        await using var stream = await service.OpenReadStreamAsync(face);
        stream.Should().NotBeNull();
        minio.Verify(m => m.GetObjectAsync(It.IsAny<GetObjectArgs>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
