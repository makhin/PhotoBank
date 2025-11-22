using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
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
    public async Task ResolveAsync_ReturnsRelativePath_WhenKeyIsProvided()
    {
        var minioClient = new Mock<IMinioClient>();
        var logger = new TestLogger<MediaUrlResolver>();
        var resolver = new MediaUrlResolver(minioClient.Object, Options.Create(new S3Options { Bucket = "photobank" }), logger);

        var result = await resolver.ResolveAsync("photos/image.jpg", 120, MediaUrlContext.ForPhoto(10));

        result.Should().Be("/minio/photobank/photos/image.jpg");
        logger.Entries.Should().ContainSingle();
        logger.Entries[0].Level.Should().Be(LogLevel.Debug);
    }

    [Test]
    public async Task ResolveAsync_ReturnsRelativePath_WithCorrectBucket()
    {
        var minioClient = new Mock<IMinioClient>();
        var logger = new TestLogger<MediaUrlResolver>();
        var resolver = new MediaUrlResolver(minioClient.Object, Options.Create(new S3Options { Bucket = "custom-bucket" }), logger);

        var result = await resolver.ResolveAsync("object-key", 120, MediaUrlContext.ForFace(99, 5));

        result.Should().Be("/minio/custom-bucket/object-key");
        logger.Entries.Should().ContainSingle();
        logger.Entries[0].Level.Should().Be(LogLevel.Debug);
    }
}
