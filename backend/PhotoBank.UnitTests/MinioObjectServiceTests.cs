using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Minio;
using Minio.DataModel.Args;
using Moq;
using NUnit.Framework;
using PhotoBank.Services;
using PhotoBank.UnitTests.Infrastructure.Minio;

namespace PhotoBank.UnitTests;

[TestFixture]
public class MinioObjectServiceTests
{
    [Test]
    public async Task GetObjectAsync_ReturnsBytesFromMinio()
    {
        var data = new byte[] { 1, 2, 3 };
        GetObjectArgs? capturedArgs = null;
        var minio = new Mock<IMinioClient>();
        minio.SetupGetObjectReturning(data, args => capturedArgs = args);

        var service = new MinioObjectService(minio.Object);
        var result = await service.GetObjectAsync("key1");

        result.Should().Equal(data);
        var bucket = (string?)capturedArgs!.GetType().GetProperty("BucketName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(capturedArgs);
        var obj = (string?)capturedArgs!.GetType().GetProperty("ObjectName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(capturedArgs);
        bucket.Should().Be("photobank");
        obj.Should().Be("key1");
    }
}
