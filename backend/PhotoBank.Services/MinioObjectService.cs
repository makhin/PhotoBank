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

    public async Task DeleteObjectAsync(string key)
    {
        await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
            .WithBucket(Bucket)
            .WithObject(key));
    }

    public async Task<bool> ObjectExistsAsync(string key)
    {
        try
        {
            await _minioClient.StatObjectAsync(new StatObjectArgs()
                .WithBucket(Bucket)
                .WithObject(key));
            return true;
        }
        catch
        {
            return false;
        }
    }
}

