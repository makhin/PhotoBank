using System.IO;
using System.Threading.Tasks;
using Minio;
using Minio.DataModel.Args;

namespace PhotoBank.Services;

public class MinioObjectService
{
    private readonly IMinioClient _minioClient;
    private const string Bucket = "photobank";

    public MinioObjectService(IMinioClient minioClient)
    {
        _minioClient = minioClient;
    }

    public async Task<byte[]> GetObjectAsync(string key)
    {
        using var ms = new MemoryStream();
        await _minioClient.GetObjectAsync(new GetObjectArgs()
            .WithBucket(Bucket)
            .WithObject(key)
            .WithCallbackStream(stream => stream.CopyTo(ms)));
        return ms.ToArray();
    }
}

