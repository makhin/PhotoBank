using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Minio;
using Minio.DataModel.Args;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Events;
using System.Linq;

// ReSharper disable once CheckNamespace
using PhotoBank.Services; // For S3KeyBuilder

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
        var info = await _context.Photos
            .AsNoTracking()
            .Where(p => p.Id == notification.PhotoId)
            .Select(p => new { Storage = p.Storage.Name, p.RelativePath })
            .SingleAsync(cancellationToken);

        var photoEntry = GetPhotoEntry(notification.PhotoId);

        var previewKey = S3KeyBuilder.BuildPreviewKey(info.Storage, info.RelativePath, notification.PhotoId);
        var (previewEtag, previewSha, previewSize) = await UploadAsync(previewKey, notification.Preview, cancellationToken);
        photoEntry.Property(p => p.S3Key_Preview).CurrentValue = previewKey;
        photoEntry.Property(p => p.S3Key_Preview).IsModified = true;
        photoEntry.Property(p => p.S3ETag_Preview).CurrentValue = previewEtag;
        photoEntry.Property(p => p.S3ETag_Preview).IsModified = true;
        photoEntry.Property(p => p.Sha256_Preview).CurrentValue = previewSha;
        photoEntry.Property(p => p.Sha256_Preview).IsModified = true;
        photoEntry.Property(p => p.BlobSize_Preview).CurrentValue = previewSize;
        photoEntry.Property(p => p.BlobSize_Preview).IsModified = true;

        if (notification.Thumbnail != null)
        {
            var thumbKey = S3KeyBuilder.BuildThumbnailKey(info.Storage, info.RelativePath, notification.PhotoId);
            var (thumbEtag, thumbSha, thumbSize) = await UploadAsync(thumbKey, notification.Thumbnail, cancellationToken);
            photoEntry.Property(p => p.S3Key_Thumbnail).CurrentValue = thumbKey;
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
            var key = S3KeyBuilder.BuildFaceKey(face.FaceId);
            var (etag, sha, size) = await UploadAsync(key, face.Image, cancellationToken);
            var faceEntry = GetFaceEntry(face.FaceId);
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

    private EntityEntry<Photo> GetPhotoEntry(int photoId)
    {
        var tracked = _context.ChangeTracker.Entries<Photo>()
            .FirstOrDefault(e => e.Entity.Id == photoId);
        if (tracked != null)
        {
            return tracked;
        }

        var local = _context.Photos.Local.FirstOrDefault(p => p.Id == photoId);
        if (local != null)
        {
            return _context.Entry(local);
        }

        return _context.Photos.Attach(new Photo { Id = photoId });
    }

    private EntityEntry<Face> GetFaceEntry(int faceId)
    {
        var tracked = _context.ChangeTracker.Entries<Face>()
            .FirstOrDefault(e => e.Entity.Id == faceId);
        if (tracked != null)
        {
            return tracked;
        }

        var local = _context.Faces.Local.FirstOrDefault(f => f.Id == faceId);
        if (local != null)
        {
            return _context.Entry(local);
        }

        return _context.Faces.Attach(new Face { Id = faceId });
    }
}
