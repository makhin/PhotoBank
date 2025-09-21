using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using Moq;
using Moq.Language.Flow;

namespace PhotoBank.UnitTests.Infrastructure.Minio;

public static class MinioMockExtensions
{
    public static IReturnsResult<IMinioClient> SetupGetObjectReturning(
        this Mock<IMinioClient> mock,
        byte[] payload,
        Action<GetObjectArgs>? onArgs = null)
    {
        return mock
            .Setup(m => m.GetObjectAsync(It.IsAny<GetObjectArgs>(), It.IsAny<CancellationToken>()))
            .Callback<GetObjectArgs, CancellationToken>((args, token) =>
            {
                onArgs?.Invoke(args);

                var callback = GetCallbackDelegate(args);
                if (callback is null)
                {
                    return;
                }

                using var stream = new MemoryStream(payload);
                callback.DynamicInvoke(stream, CancellationToken.None);
            })
            .ReturnsAsync(CreateObjectStat());
    }

    private static Delegate? GetCallbackDelegate(GetObjectArgs args)
    {
        return args
            .GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(f => typeof(Delegate).IsAssignableFrom(f.FieldType))
            .Select(f => f.GetValue(args) as Delegate)
            .FirstOrDefault(d => d is not null);
    }

    private static ObjectStat CreateObjectStat()
    {
        return (ObjectStat)Activator.CreateInstance(typeof(ObjectStat), nonPublic: true)!;
    }
}
