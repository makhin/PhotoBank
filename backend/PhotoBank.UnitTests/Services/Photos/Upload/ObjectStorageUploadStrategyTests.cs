using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.DataModel.Response;
using Moq;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Photos.Upload;
using PhotoBank.Services.Internal;

namespace PhotoBank.UnitTests.Services.Photos.Upload;

[TestFixture]
public class ObjectStorageUploadStrategyTests
{
    private static IFormFile CreateFormFile(byte[] content, string fileName, string? contentType = null)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static ObjectStat CreateObjectStat(long size)
    {
        var stat = (ObjectStat)FormatterServices.GetUninitializedObject(typeof(ObjectStat));
        typeof(ObjectStat).GetProperty("Size")!.SetValue(stat, size);
        return stat;
    }

    [Test]
    public void CanHandle_ReturnsTrue_ForS3Uri()
    {
        var strategy = CreateStrategy();
        var storage = new Storage { Folder = "s3://bucket/root" };

        strategy.CanHandle(storage).Should().BeTrue();
    }

    [Test]
    public async Task UploadAsync_UploadsObjectWithResolvedKey()
    {
        var minio = new Mock<IMinioClient>();
        minio
            .Setup(m => m.StatObjectAsync(It.IsAny<StatObjectArgs>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("not found"));

        string? capturedBucket = null;
        string? capturedKey = null;

        minio
            .Setup(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectArgs, CancellationToken>((args, _) =>
            {
                capturedBucket = ReadStringProperty(args, "BucketName");
                capturedKey = ReadStringProperty(args, "ObjectName");
            })
            .Returns(Task.FromResult(CreatePutObjectResponse()));

        var strategy = CreateStrategy(minioClient: minio.Object);
        var storage = new Storage { Id = 5, Folder = "s3://photos/base" };
        var file = CreateFormFile(new byte[] { 1, 2, 3 }, "image.jpg", "image/jpeg");

        await strategy.UploadAsync(storage, new[] { file }, "nested", CancellationToken.None);

        capturedBucket.Should().Be("photos");
        capturedKey.Should().Be("base/nested/image.jpg");
    }

    [Test]
    public async Task UploadAsync_SkipsDuplicateWhenSameSize()
    {
        var minio = new Mock<IMinioClient>();
        minio
            .Setup(m => m.StatObjectAsync(It.IsAny<StatObjectArgs>(), It.IsAny<CancellationToken>()))
            .Returns<StatObjectArgs, CancellationToken>((_, _) =>
                Task.FromResult(CreateObjectStat(3)));

        var strategy = CreateStrategy(minioClient: minio.Object);
        var storage = new Storage { Id = 6, Folder = "s3://bucket/root" };
        var file = CreateFormFile(new byte[] { 1, 2, 3 }, "image.jpg");

        await strategy.UploadAsync(storage, new[] { file }, null, CancellationToken.None);

        minio.Verify(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task UploadAsync_AppendsSuffixWhenDifferentSize()
    {
        var minio = new Mock<IMinioClient>();
        minio
            .Setup(m => m.StatObjectAsync(It.IsAny<StatObjectArgs>(), It.IsAny<CancellationToken>()))
            .Returns<StatObjectArgs, CancellationToken>((args, _) =>
            {
                var objectName = ReadStringProperty(args, "ObjectName");
                if (objectName?.EndsWith("image.jpg", StringComparison.Ordinal) == true)
                {
                    return Task.FromResult(CreateObjectStat(5));
                }

                return Task.FromException<ObjectStat>(new Exception("not found"));
            });

        string? capturedKey = null;
        minio
            .Setup(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectArgs, CancellationToken>((args, _) =>
            {
                capturedKey = ReadStringProperty(args, "ObjectName");
            })
            .Returns(Task.FromResult(CreatePutObjectResponse()));

        var strategy = CreateStrategy(minioClient: minio.Object);
        var storage = new Storage { Id = 7, Folder = "s3://bucket/root" };
        var file = CreateFormFile(new byte[] { 1, 2, 3 }, "image.jpg");

        await strategy.UploadAsync(storage, new[] { file }, null, CancellationToken.None);

        capturedKey.Should().Be("root/image_1.jpg");
    }

    private static ObjectStorageUploadStrategy CreateStrategy(IMinioClient? minioClient = null)
    {
        var client = minioClient ?? Mock.Of<IMinioClient>();
        var options = Options.Create(new S3Options { Bucket = "default" });
        return new ObjectStorageUploadStrategy(
            client,
            options,
            new UploadNameResolver(),
            NullLogger<ObjectStorageUploadStrategy>.Instance);
    }

    private static string? ReadStringProperty(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return property?.GetValue(instance) as string;
    }

    private static PutObjectResponse CreatePutObjectResponse()
        => (PutObjectResponse)FormatterServices.GetUninitializedObject(typeof(PutObjectResponse));
}
