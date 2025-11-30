using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.DataModel.Response;
using Moq;
using NUnit.Framework;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Events;
using PhotoBank.Services.Handlers;

namespace PhotoBank.UnitTests.Handlers;

[TestFixture]
public class PhotoCreatedHandlerTests
{
    private PhotoBankDbContext _context = null!;
    private Mock<IMinioClient> _minioMock = null!;
    private PhotoCreatedHandler _handler = null!;
    private const int PhotoId = 1;
    private const int FaceId = 100;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbFactory.CreateInMemory();
        _minioMock = new Mock<IMinioClient>(MockBehavior.Strict);
        _handler = new PhotoCreatedHandler(_context, _minioMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task Handle_ShouldBeIdempotent_WhenEventIsRedelivered()
    {
        SeedPhotoAndFace();

        ConfigureSuccessfulStorage(new[]
        {
            "preview-etag-1", "thumbnail-etag-1", "face-etag-1",
            "preview-etag-2", "thumbnail-etag-2", "face-etag-2"
        });

        var preview = new byte[] { 1, 2, 3, 4 };
        var thumbnail = new byte[] { 5, 6, 7 };
        var faceImage = new byte[] { 8, 9, 10 };
        var notification = new PhotoCreated(
            PhotoId,
            "storage",
            "album",
            preview,
            thumbnail,
            new[] { new PhotoCreatedFace(FaceId, faceImage) });

        await _handler.Handle(notification, CancellationToken.None);
        await _handler.Handle(notification, CancellationToken.None);

        var savedPhoto = await _context.Photos.AsNoTracking().SingleAsync(p => p.Id == PhotoId);
        var savedFace = await _context.Faces.AsNoTracking().SingleAsync(f => f.Id == FaceId);

        savedPhoto.S3Key_Preview.Should().Be("preview/storage/album/0000000001_preview.jpg");
        savedPhoto.S3ETag_Preview.Should().Be("preview-etag-2");
        savedPhoto.Sha256_Preview.Should().Be(ComputeSha(preview));
        savedPhoto.BlobSize_Preview.Should().Be(preview.Length);

        savedPhoto.S3Key_Thumbnail.Should().Be("thumbnail/storage/album/0000000001_thumbnail.jpg");
        savedPhoto.S3ETag_Thumbnail.Should().Be("thumbnail-etag-2");
        savedPhoto.Sha256_Thumbnail.Should().Be(ComputeSha(thumbnail));
        savedPhoto.BlobSize_Thumbnail.Should().Be(thumbnail.Length);

        savedFace.S3Key_Image.Should().Be("faces/0000000100.jpg");
        savedFace.S3ETag_Image.Should().Be("face-etag-2");
        savedFace.Sha256_Image.Should().Be(ComputeSha(faceImage));
        savedFace.BlobSize_Image.Should().Be(faceImage.Length);

        _minioMock.Verify(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()), Times.Exactly(6));
        _minioMock.Verify(m => m.StatObjectAsync(It.IsAny<StatObjectArgs>(), It.IsAny<CancellationToken>()), Times.Exactly(6));
        _minioMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Handle_ShouldSkipThumbnail_WhenPayloadDoesNotContainIt()
    {
        SeedPhotoAndFace(photo =>
        {
            photo.S3Key_Thumbnail = "existing-thumb";
            photo.S3ETag_Thumbnail = "existing-etag";
            photo.Sha256_Thumbnail = "existing-sha";
            photo.BlobSize_Thumbnail = 123;
        });

        ConfigureSuccessfulStorage(new[] { "preview-etag", "face-etag" });

        var preview = new byte[] { 42, 42, 42 };
        var faceImage = new byte[] { 7, 7, 7 };
        var notification = new PhotoCreated(
            PhotoId,
            "storage",
            "album",
            preview,
            null,
            new[] { new PhotoCreatedFace(FaceId, faceImage) });

        await _handler.Handle(notification, CancellationToken.None);

        var savedPhoto = await _context.Photos.AsNoTracking().SingleAsync(p => p.Id == PhotoId);
        var savedFace = await _context.Faces.AsNoTracking().SingleAsync(f => f.Id == FaceId);

        savedPhoto.S3Key_Preview.Should().Be("preview/storage/album/0000000001_preview.jpg");
        savedPhoto.S3ETag_Preview.Should().Be("preview-etag");
        savedPhoto.Sha256_Preview.Should().Be(ComputeSha(preview));
        savedPhoto.BlobSize_Preview.Should().Be(preview.Length);

        savedPhoto.S3Key_Thumbnail.Should().Be("existing-thumb");
        savedPhoto.S3ETag_Thumbnail.Should().Be("existing-etag");
        savedPhoto.Sha256_Thumbnail.Should().Be("existing-sha");
        savedPhoto.BlobSize_Thumbnail.Should().Be(123);

        savedFace.S3Key_Image.Should().Be("faces/0000000100.jpg");
        savedFace.S3ETag_Image.Should().Be("face-etag");
        savedFace.Sha256_Image.Should().Be(ComputeSha(faceImage));
        savedFace.BlobSize_Image.Should().Be(faceImage.Length);

        _minioMock.Verify(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _minioMock.Verify(m => m.StatObjectAsync(It.IsAny<StatObjectArgs>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _minioMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Handle_ShouldAllowExternalRetry_WhenStorageFailsInitially()
    {
        SeedPhotoAndFace();

        var preview = new byte[] { 1, 1, 1, 1 };
        var thumbnail = new byte[] { 2, 2, 2, 2 };
        var faceImage = new byte[] { 3, 3, 3, 3 };
        var notification = new PhotoCreated(
            PhotoId,
            "storage",
            "album",
            preview,
            thumbnail,
            new[] { new PhotoCreatedFace(FaceId, faceImage) });

        ConfigureTransientFailure(new[] { "preview-etag", "thumbnail-etag", "face-etag" });

        var firstAttempt = () => _handler.Handle(notification, CancellationToken.None);
        await firstAttempt.Should().ThrowAsync<IOException>();

        _context.ChangeTracker.Clear();

        await _handler.Handle(notification, CancellationToken.None);

        var savedPhoto = await _context.Photos.AsNoTracking().SingleAsync(p => p.Id == PhotoId);
        var savedFace = await _context.Faces.AsNoTracking().SingleAsync(f => f.Id == FaceId);

        savedPhoto.S3Key_Preview.Should().Be("preview/storage/album/0000000001_preview.jpg");
        savedPhoto.S3ETag_Preview.Should().Be("preview-etag");
        savedPhoto.Sha256_Preview.Should().Be(ComputeSha(preview));
        savedPhoto.BlobSize_Preview.Should().Be(preview.Length);

        savedPhoto.S3Key_Thumbnail.Should().Be("thumbnail/storage/album/0000000001_thumbnail.jpg");
        savedPhoto.S3ETag_Thumbnail.Should().Be("thumbnail-etag");
        savedPhoto.Sha256_Thumbnail.Should().Be(ComputeSha(thumbnail));
        savedPhoto.BlobSize_Thumbnail.Should().Be(thumbnail.Length);

        savedFace.S3Key_Image.Should().Be("faces/0000000100.jpg");
        savedFace.S3ETag_Image.Should().Be("face-etag");
        savedFace.Sha256_Image.Should().Be(ComputeSha(faceImage));
        savedFace.BlobSize_Image.Should().Be(faceImage.Length);

        _minioMock.Verify(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()), Times.Exactly(4));
        _minioMock.Verify(m => m.StatObjectAsync(It.IsAny<StatObjectArgs>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        _minioMock.VerifyNoOtherCalls();
    }

    private void SeedPhotoAndFace(Action<Photo>? customizePhoto = null)
    {
        var storage = new Storage
        {
            Id = 1,
            Name = "storage",
            Folder = "root",
            Photos = new List<Photo>()
        };

        var photo = new Photo
        {
            Id = PhotoId,
            StorageId = storage.Id,
            Storage = storage,
            RelativePath = "album",
            Name = "photo",
            AccentColor = string.Empty,
            DominantColorBackground = string.Empty,
            DominantColorForeground = string.Empty,
            DominantColors = string.Empty,
            ImageHash = string.Empty,
            S3Key_Preview = string.Empty,
            S3ETag_Preview = string.Empty,
            Sha256_Preview = string.Empty,
            S3Key_Thumbnail = string.Empty,
            S3ETag_Thumbnail = string.Empty,
            Sha256_Thumbnail = string.Empty,
            Captions = new List<Caption>(),
            PhotoTags = new List<PhotoTag>(),
            PhotoCategories = new List<PhotoCategory>(),
            ObjectProperties = new List<ObjectProperty>(),
            Faces = new List<Face>(),
            Files = new List<PhotoBank.DbContext.Models.File>()
        };
        storage.Photos.Add(photo);

        customizePhoto?.Invoke(photo);

        var face = new Face
        {
            Id = FaceId,
            PhotoId = photo.Id,
            Photo = photo,
            Rectangle = new Point(0, 0),
            S3Key_Image = string.Empty,
            S3ETag_Image = string.Empty,
            Sha256_Image = string.Empty,
            FaceAttributes = string.Empty
        };
        photo.Faces.Add(face);

        _context.Storages.Add(storage);
        _context.Photos.Add(photo);
        _context.Faces.Add(face);
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    private void ConfigureSuccessfulStorage(IReadOnlyCollection<string> etags)
    {
        var etagQueue = new Queue<string>(etags);

        _minioMock.Setup(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PutObjectResponse)null!);

        _minioMock.Setup(m => m.StatObjectAsync(It.IsAny<StatObjectArgs>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(CreateObjectStat(etagQueue.Dequeue())));
    }

    private void ConfigureTransientFailure(IReadOnlyCollection<string> successEtags)
    {
        var etagQueue = new Queue<string>(successEtags);
        var callCount = 0;

        _minioMock.Setup(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()))
            .Returns<PutObjectArgs, CancellationToken>((_, _) =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return Task.FromException<PutObjectResponse>(new IOException("transient"));
                }

                return Task.FromResult((PutObjectResponse)null!);
            });

        _minioMock.Setup(m => m.StatObjectAsync(It.IsAny<StatObjectArgs>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(CreateObjectStat(etagQueue.Dequeue())));
    }

    private static string ComputeSha(byte[] data)
    {
        return Convert.ToHexString(SHA256.HashData(data));
    }

    private static ObjectStat CreateObjectStat(string etag)
    {
        var stat = (ObjectStat)Activator.CreateInstance(typeof(ObjectStat), true)!;
        typeof(ObjectStat).GetProperty("ETag")!.SetValue(stat, etag);
        return stat;
    }
}
