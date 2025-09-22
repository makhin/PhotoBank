using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services;
using PhotoBank.Services.Api;
using Minio;

namespace PhotoBank.UnitTests;

[TestFixture]
public class S3ResourceServiceTests
{
    [Test]
    public async Task GetAsync_PresignedUrlReturned()
    {
        var photo = new Photo { Id = 1, StorageId = 1, Storage = new Storage { Id = 1, Name = "s", Folder = "f" }, Name = "n", S3Key_Preview = "k", S3ETag_Preview = "e" };
        var repo = new Mock<IRepository<Photo>>();
        repo.Setup(r => r.GetAll())
            .Returns(new[] { photo }.AsQueryable());

        var service = new TestS3ResourceService(_ => "url", _ => Array.Empty<byte>());
        var result = await service.GetAsync(repo.Object, 1, p => p.S3Key_Preview, p => p.S3ETag_Preview);

        result.Should().NotBeNull();
        result!.PreSignedUrl.Should().Be("url");
        result.Data.Should().BeNull();
        result.ETag.Should().Be("e");
    }

    [Test]
    public async Task GetAsync_FallsBackToData()
    {
        var photo = new Photo { Id = 2, StorageId = 1, Storage = new Storage { Id = 1, Name = "s", Folder = "f" }, Name = "n", S3Key_Preview = "k2", S3ETag_Preview = "e2" };
        var repo = new Mock<IRepository<Photo>>();
        repo.Setup(r => r.GetAll())
            .Returns(new[] { photo }.AsQueryable());

        var service = new TestS3ResourceService(_ => null, _ => new byte[] {1,2,3});
        var result = await service.GetAsync(repo.Object, 2, p => p.S3Key_Preview, p => p.S3ETag_Preview);

        result.Should().NotBeNull();
        result!.PreSignedUrl.Should().BeNull();
        result.Data.Should().Equal(1,2,3);
        result.ETag.Should().Be("e2");
    }
}

class TestS3ResourceService : S3ResourceService
{
    private readonly Func<string, string?> _presign;
    private readonly Func<string, byte[]> _get;

    public TestS3ResourceService(Func<string, string?> presign, Func<string, byte[]> get)
        : base(new Mock<IMinioClient>().Object)
    {
        _presign = presign;
        _get = get;
    }

    protected override Task<string?> GetPresignedUrlAsync(string key) => Task.FromResult(_presign(key));

    protected override Task<byte[]> GetObjectAsync(string key) => Task.FromResult(_get(key));
}
