using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Minio;
using Minio.DataModel.Args;
using PhotoBank.DbContext.Models;

namespace PhotoBank.Services;

public interface IFaceStorageService
{
    Task<Stream> OpenReadStreamAsync(Face face, CancellationToken ct = default);
}

public class FaceStorageService : IFaceStorageService
{
    private readonly IMinioClient _minio;
    private const string Bucket = "photobank";

    public FaceStorageService(IMinioClient minio)
    {
        _minio = minio;
    }

    public async Task<Stream> OpenReadStreamAsync(Face face, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(face.S3Key_Image))
            throw new InvalidOperationException("Face has no image data");

        var ms = new MemoryStream();
        await _minio.GetObjectAsync(new GetObjectArgs()
            .WithBucket(Bucket)
            .WithObject(face.S3Key_Image)
            .WithCallbackStream(async (stream, token) => await stream.CopyToAsync(ms, token)), ct);
        ms.Position = 0;
        return ms;
    }
}
