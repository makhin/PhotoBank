using System;
using System.Collections.Generic;
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
using System.Linq.Expressions;

// ReSharper disable once CheckNamespace

// For S3KeyBuilder

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

        var photoEntry = GetEntityEntry(p => p.Id == notification.PhotoId, () => new Photo { Id = notification.PhotoId });
        var uploadedKeys = new List<string>();

        try
        {
            // Upload preview to S3
            var previewKey = S3KeyBuilder.BuildPreviewKey(info.Storage, info.RelativePath, notification.PhotoId);
            await UploadAndSetBlobProperties(previewKey, notification.Preview, photoEntry, uploadedKeys, cancellationToken,
                p => p.S3Key_Preview, p => p.S3ETag_Preview, p => p.Sha256_Preview, p => p.BlobSize_Preview);

            // Upload thumbnail to S3 if exists
            if (notification.Thumbnail != null)
            {
                var thumbKey = S3KeyBuilder.BuildThumbnailKey(info.Storage, info.RelativePath, notification.PhotoId);
                await UploadAndSetBlobProperties(thumbKey, notification.Thumbnail, photoEntry, uploadedKeys, cancellationToken,
                    p => p.S3Key_Thumbnail, p => p.S3ETag_Thumbnail, p => p.Sha256_Thumbnail, p => p.BlobSize_Thumbnail);
            }

            // Upload face images to S3
            foreach (var face in notification.Faces)
            {
                var key = S3KeyBuilder.BuildFaceKey(face.FaceId);
                var faceEntry = GetEntityEntry(f => f.Id == face.FaceId, () => new Face { Id = face.FaceId });
                await UploadAndSetBlobProperties(key, face.Image, faceEntry, uploadedKeys, cancellationToken,
                    f => f.S3Key_Image, f => f.S3ETag_Image, f => f.Sha256_Image, f => f.BlobSize_Image);
            }

            // Save changes to database
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            // Rollback: Delete all uploaded S3 objects if database save fails
            await CleanupS3ObjectsAsync(uploadedKeys, cancellationToken);
            throw;
        }
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

    private async Task CleanupS3ObjectsAsync(List<string> keys, CancellationToken ct)
    {
        foreach (var key in keys)
        {
            try
            {
                await _minio.RemoveObjectAsync(new RemoveObjectArgs()
                    .WithBucket("photobank")
                    .WithObject(key), ct);
            }
            catch
            {
                // Ignore cleanup errors - this is best-effort rollback
                // Orphaned objects can be cleaned up later by a maintenance job
            }
        }
    }

    private async Task UploadAndSetBlobProperties<T>(
        string key,
        byte[] data,
        EntityEntry<T> entry,
        List<string> uploadedKeys,
        CancellationToken cancellationToken,
        Expression<Func<T, string?>> s3KeyProp,
        Expression<Func<T, string?>> etagProp,
        Expression<Func<T, string?>> shaProp,
        Expression<Func<T, long?>> sizeProp) where T : class
    {
        var (etag, sha, size) = await UploadAsync(key, data, cancellationToken);
        uploadedKeys.Add(key);
        SetBlobProperties(entry, key, etag, sha, size, s3KeyProp, etagProp, shaProp, sizeProp);
    }

    private static void SetBlobProperties<T>(EntityEntry<T> entry, string s3Key, string etag, string sha, long size,
        Expression<Func<T, string?>> s3KeyProp, Expression<Func<T, string?>> etagProp,
        Expression<Func<T, string?>> shaProp, Expression<Func<T, long?>> sizeProp) where T : class
    {
        entry.Property(s3KeyProp).CurrentValue = s3Key;
        entry.Property(s3KeyProp).IsModified = true;
        entry.Property(etagProp).CurrentValue = etag;
        entry.Property(etagProp).IsModified = true;
        entry.Property(shaProp).CurrentValue = sha;
        entry.Property(shaProp).IsModified = true;
        entry.Property(sizeProp).CurrentValue = size;
        entry.Property(sizeProp).IsModified = true;
    }

    private EntityEntry<T> GetEntityEntry<T>(Func<T, bool> predicate, Func<T> factory) where T : class
    {
        var tracked = _context.ChangeTracker.Entries<T>()
            .FirstOrDefault(e => predicate(e.Entity));
        if (tracked != null)
        {
            return tracked;
        }

        var local = _context.Set<T>().Local.FirstOrDefault(predicate);
        return local != null ? _context.Entry(local) : _context.Attach(factory());
    }
}
