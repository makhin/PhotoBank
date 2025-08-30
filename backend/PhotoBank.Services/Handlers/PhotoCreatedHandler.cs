using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel.Args;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Events;

namespace PhotoBank.Services.Handlers;

public class PhotoCreatedHandler : INotificationHandler<PhotoCreated>
{
    private readonly PhotoBankDbContext _context;
    private readonly IMinioClient _minio;

    public PhotoCreatedHandler(PhotoBankDbContext context, IMinioClient minio)
    {
        _context = context;
        _minio = minio;
    }

    public async Task Handle(PhotoCreated notification, CancellationToken cancellationToken)
    {
        var photoEntry = _context.Photos.Attach(new Photo { Id = notification.PhotoId });

        var (previewEtag, previewSha, previewSize) = await UploadAsync($"previews/{notification.PhotoId}.jpg", notification.Preview, cancellationToken);
        photoEntry.Property(p => p.S3Key_Preview).CurrentValue = $"previews/{notification.PhotoId}.jpg";
        photoEntry.Property(p => p.S3Key_Preview).IsModified = true;
        photoEntry.Property(p => p.S3ETag_Preview).CurrentValue = previewEtag;
        photoEntry.Property(p => p.S3ETag_Preview).IsModified = true;
        photoEntry.Property(p => p.Sha256_Preview).CurrentValue = previewSha;
        photoEntry.Property(p => p.Sha256_Preview).IsModified = true;
        photoEntry.Property(p => p.BlobSize_Preview).CurrentValue = previewSize;
        photoEntry.Property(p => p.BlobSize_Preview).IsModified = true;

        if (notification.Thumbnail != null)
        {
            var (thumbEtag, thumbSha, thumbSize) = await UploadAsync($"thumbnails/{notification.PhotoId}.jpg", notification.Thumbnail, cancellationToken);
            photoEntry.Property(p => p.S3Key_Thumbnail).CurrentValue = $"thumbnails/{notification.PhotoId}.jpg";
            photoEntry.Property(p => p.S3Key_Thumbnail).IsModified = true;
            photoEntry.Property(p => p.S3ETag_Thumbnail).CurrentValue = thumbEtag;
            photoEntry.Property(p => p.S3ETag_Thumbnail).IsModified = true;
            photoEntry.Property(p => p.Sha256_Thumbnail).CurrentValue = thumbSha;
            photoEntry.Property(p => p.Sha256_Thumbnail).IsModified = true;
            photoEntry.Property(p => p.BlobSize_Thumbnail).CurrentValue = thumbSize;
            photoEntry.Property(p => p.BlobSize_Thumbnail).IsModified = true;
        }

        foreach (var face in notification.Faces)
        {
            var key = $"faces/{notification.PhotoId}_{face.FaceId}.jpg";
            var (etag, sha, size) = await UploadAsync(key, face.Image, cancellationToken);
            var faceEntry = _context.Faces.Attach(new Face { Id = face.FaceId });
            faceEntry.Property(f => f.S3Key_Image).CurrentValue = key;
            faceEntry.Property(f => f.S3Key_Image).IsModified = true;
            faceEntry.Property(f => f.S3ETag_Image).CurrentValue = etag;
            faceEntry.Property(f => f.S3ETag_Image).IsModified = true;
            faceEntry.Property(f => f.Sha256_Image).CurrentValue = sha;
            faceEntry.Property(f => f.Sha256_Image).IsModified = true;
            faceEntry.Property(f => f.BlobSize_Image).CurrentValue = size;
            faceEntry.Property(f => f.BlobSize_Image).IsModified = true;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<(string etag, string sha, long size)> UploadAsync(string key, byte[] data, CancellationToken ct)
    {
        await using var ms = new MemoryStream(data);
        string sha;
        using (var hasher = SHA256.Create())
        {
            sha = Convert.ToHexString(await hasher.ComputeHashAsync(ms, ct));
        }
        ms.Position = 0;
        await _minio.PutObjectAsync(new PutObjectArgs()
            .WithBucket("photobank")
            .WithObject(key)
            .WithStreamData(ms)
            .WithObjectSize(ms.Length)
            .WithContentType("image/jpeg"), ct);
        var stat = await _minio.StatObjectAsync(new StatObjectArgs().WithBucket("photobank").WithObject(key), ct);
        return (stat.ETag ?? string.Empty, sha, ms.Length);
    }
}
