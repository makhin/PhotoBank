using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using Moq;
using NUnit.Framework;
using PhotoBank.Services;

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
        minio.Setup(m => m.GetObjectAsync(It.IsAny<GetObjectArgs>(), It.IsAny<CancellationToken>()))
            .Callback<GetObjectArgs, CancellationToken>((args, ct) =>
            {
                capturedArgs = args;
                var field = args.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                    .FirstOrDefault(f => typeof(Delegate).IsAssignableFrom(f.FieldType));
                var del = field?.GetValue(args) as Delegate;
                del?.DynamicInvoke(new MemoryStream(data));
            })
            .ReturnsAsync((ObjectStat)Activator.CreateInstance(typeof(ObjectStat), nonPublic: true)!);

        var service = new MinioObjectService(minio.Object);
        var result = await service.GetObjectAsync("key1");

        result.Should().Equal(data);
        var bucket = (string?)capturedArgs!.GetType().GetProperty("BucketName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(capturedArgs);
        var obj = (string?)capturedArgs!.GetType().GetProperty("ObjectName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(capturedArgs);
        bucket.Should().Be("photobank");
        obj.Should().Be("key1");
    }
}
