using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Moq;
using NUnit.Framework;
using PhotoBank.Services.Internal;
using PhotoBank.UnitTests.Infrastructure.Logging;

namespace PhotoBank.UnitTests.Internal;

[TestFixture]
public sealed class MediaUrlResolverTests
{
    [Test]
    public async Task ResolveAsync_ReturnsNull_WhenKeyIsMissing()
    {
        var minioClient = new Mock<IMinioClient>(MockBehavior.Strict);
        var logger = new TestLogger<MediaUrlResolver>();
        var resolver = new MediaUrlResolver(minioClient.Object, Options.Create(new S3Options { Bucket = "bucket" }), logger);

        var result = await resolver.ResolveAsync(null, 60, MediaUrlContext.ForPhoto(42));

        result.Should().BeNull();
        minioClient.VerifyNoOtherCalls();
        logger.Entries.Should().BeEmpty();
    }

    [Test]
    public async Task ResolveAsync_ReturnsUrl_WhenPresignedSuccessfully()
    {
        const string expectedUrl = "https://example.com/object";
        var minioClient = new Mock<IMinioClient>();
        minioClient
            .Setup(c => c.PresignedGetObjectAsync(It.IsAny<PresignedGetObjectArgs>()))
            .ReturnsAsync(expectedUrl);
        var logger = new TestLogger<MediaUrlResolver>();
        var resolver = new MediaUrlResolver(minioClient.Object, Options.Create(new S3Options { Bucket = "bucket" }), logger);

        var result = await resolver.ResolveAsync("object-key", 120, MediaUrlContext.ForPhoto(10));

        result.Should().Be(expectedUrl);
        logger.Entries.Should().BeEmpty();
        minioClient.Verify(c => c.PresignedGetObjectAsync(It.IsAny<PresignedGetObjectArgs>()), Times.Once);
    }

    [Test]
    public async Task ResolveAsync_LogsWarningAndReturnsNull_WhenPresignedFails()
    {
        var minioClient = new Mock<IMinioClient>();
        minioClient
            .Setup(c => c.PresignedGetObjectAsync(It.IsAny<PresignedGetObjectArgs>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var logger = new TestLogger<MediaUrlResolver>();
        var resolver = new MediaUrlResolver(minioClient.Object, Options.Create(new S3Options { Bucket = "bucket" }), logger);

        var result = await resolver.ResolveAsync("object-key", 120, MediaUrlContext.ForFace(99, 5));

        result.Should().BeNull();
        logger.Entries.Should().ContainSingle();
        logger.Entries[0].Level.Should().Be(LogLevel.Warning);
        logger.Entries[0].Message.Should().Contain("Failed to generate presigned URL");
        logger.Entries[0].Exception.Should().BeOfType<InvalidOperationException>();
    }
}
