using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minio;
using Moq;
using NUnit.Framework;
using PhotoBank.AccessControl;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services;
using PhotoBank.Services.Api;
using PhotoBank.Services.Internal;
using PhotoBank.Services.Search;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.UnitTests.Services;

[TestFixture]
public class PhotoServiceS3AccessTests
{
    private IMapper _mapper = null!;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddProfile(new MappingProfile()));

        var provider = services.BuildServiceProvider();
        _mapper = provider.GetRequiredService<IMapper>();
    }

    [Test]
    public async Task GetPhotoPreviewAsync_ReturnsNull_ForDisallowedUser()
    {
        var dbName = Guid.NewGuid().ToString();
        using var provider = BuildProvider(dbName);
        var context = provider.GetRequiredService<PhotoBankDbContext>();

        var storage = new Storage { Id = 1, Name = "s1", Folder = "folder" };
        context.Storages.Add(storage);

        var photo = new Photo
        {
            Id = 10,
            StorageId = storage.Id,
            Storage = storage,
            Name = "photo",
            AccentColor = "000000",
            DominantColorBackground = "000000",
            DominantColorForeground = "000000",
            DominantColors = "[]",
            ImageHash = "hash",
            RelativePath = "path",
            Faces = new List<Face>(),
            Captions = new List<Caption>(),
            PhotoTags = new List<PhotoTag>(),
            PhotoCategories = new List<PhotoCategory>(),
            ObjectProperties = new List<ObjectProperty>(),
            Files = new List<File>(),
            S3Key_Preview = "preview-key",
            S3ETag_Preview = "preview-etag",
            Sha256_Preview = "sha-preview",
            S3Key_Thumbnail = "thumb-key",
            S3ETag_Thumbnail = "thumb-etag",
            Sha256_Thumbnail = "sha-thumb"
        };
        context.Photos.Add(photo);
        await context.SaveChangesAsync();

        var user = new TestCurrentUser(
            isAdmin: false,
            allowedStorageIds: new[] { storage.Id + 1 },
            allowedPersonGroupIds: Array.Empty<int>());

        var service = CreateService(provider, context, user);

        var preview = await service.GetPhotoPreviewAsync(photo.Id);
        var thumbnail = await service.GetPhotoThumbnailAsync(photo.Id);

        preview.Should().BeNull();
        thumbnail.Should().BeNull();
    }

    [Test]
    public async Task GetPhotoPreviewAsync_ReturnsResult_ForAuthorizedUser()
    {
        var dbName = Guid.NewGuid().ToString();
        using var provider = BuildProvider(dbName);
        var context = provider.GetRequiredService<PhotoBankDbContext>();

        var storage = new Storage { Id = 2, Name = "s2", Folder = "folder" };
        context.Storages.Add(storage);

        var photo = new Photo
        {
            Id = 20,
            StorageId = storage.Id,
            Storage = storage,
            Name = "photo",
            AccentColor = "000000",
            DominantColorBackground = "000000",
            DominantColorForeground = "000000",
            DominantColors = "[]",
            ImageHash = "hash",
            RelativePath = "path",
            Faces = new List<Face>(),
            Captions = new List<Caption>(),
            PhotoTags = new List<PhotoTag>(),
            PhotoCategories = new List<PhotoCategory>(),
            ObjectProperties = new List<ObjectProperty>(),
            Files = new List<File>(),
            S3Key_Preview = "preview-key",
            S3ETag_Preview = "preview-etag",
            Sha256_Preview = "sha-preview",
            S3Key_Thumbnail = "thumb-key",
            S3ETag_Thumbnail = "thumb-etag",
            Sha256_Thumbnail = "sha-thumb"
        };
        context.Photos.Add(photo);
        await context.SaveChangesAsync();

        var user = new TestCurrentUser(
            isAdmin: false,
            allowedStorageIds: new[] { storage.Id },
            allowedPersonGroupIds: Array.Empty<int>());

        var service = CreateService(provider, context, user);

        var preview = await service.GetPhotoPreviewAsync(photo.Id);
        var thumbnail = await service.GetPhotoThumbnailAsync(photo.Id);

        preview.Should().NotBeNull();
        preview!.ETag.Should().Be(photo.S3ETag_Preview);
        preview.PreSignedUrl.Should().Be("preview-key-url");
        thumbnail.Should().NotBeNull();
        thumbnail!.ETag.Should().Be(photo.S3ETag_Thumbnail);
        thumbnail.PreSignedUrl.Should().Be("thumb-key-url");
    }

    [Test]
    public async Task GetFaceImageAsync_RespectsPhotoAcl()
    {
        var dbName = Guid.NewGuid().ToString();
        using var provider = BuildProvider(dbName);
        var context = provider.GetRequiredService<PhotoBankDbContext>();

        var storage = new Storage { Id = 3, Name = "s3", Folder = "folder" };
        var group = new PersonGroup { Id = 5, Name = "group" };
        var person = new Person
        {
            Id = 7,
            Name = "person",
            PersonGroups = new List<PersonGroup> { group }
        };
        group.Persons = new List<Person> { person };

        context.Storages.Add(storage);
        context.PersonGroups.Add(group);
        context.Persons.Add(person);

        var photo = new Photo
        {
            Id = 30,
            StorageId = storage.Id,
            Storage = storage,
            Name = "photo",
            AccentColor = "000000",
            DominantColorBackground = "000000",
            DominantColorForeground = "000000",
            DominantColors = "[]",
            ImageHash = "hash",
            RelativePath = "path",
            Faces = new List<Face>(),
            Captions = new List<Caption>(),
            PhotoTags = new List<PhotoTag>(),
            PhotoCategories = new List<PhotoCategory>(),
            ObjectProperties = new List<ObjectProperty>(),
            Files = new List<File>(),
            S3Key_Preview = "preview-key",
            S3ETag_Preview = "preview-etag",
            Sha256_Preview = "sha-preview",
            S3Key_Thumbnail = "thumb-key",
            S3ETag_Thumbnail = "thumb-etag",
            Sha256_Thumbnail = "sha-thumb"
        };
        context.Photos.Add(photo);

        var face = new Face
        {
            Id = 11,
            Photo = photo,
            PhotoId = photo.Id,
            Person = person,
            PersonId = person.Id,
            Rectangle = new NetTopologySuite.Geometries.Point(0, 0),
            S3Key_Image = "face-key",
            S3ETag_Image = "face-etag",
            Sha256_Image = "face-sha",
            FaceAttributes = "{}",
            PersonFace = null!
        };
        photo.Faces.Add(face);
        context.Faces.Add(face);

        await context.SaveChangesAsync();

        var disallowedUser = new TestCurrentUser(
            isAdmin: false,
            allowedStorageIds: new[] { storage.Id + 1 },
            allowedPersonGroupIds: new[] { group.Id });

        var disallowedService = CreateService(provider, context, disallowedUser);
        var denied = await disallowedService.GetFaceImageAsync(face.Id);
        denied.Should().BeNull();

        var allowedUser = new TestCurrentUser(
            isAdmin: false,
            allowedStorageIds: new[] { storage.Id },
            allowedPersonGroupIds: new[] { group.Id });

        var allowedService = CreateService(provider, context, allowedUser);
        var allowed = await allowedService.GetFaceImageAsync(face.Id);

        allowed.Should().NotBeNull();
        allowed!.ETag.Should().Be(face.S3ETag_Image);
        allowed.PreSignedUrl.Should().Be("face-key-url");
    }

    private PhotoService CreateService(
        IServiceProvider provider,
        PhotoBankDbContext context,
        ICurrentUser currentUser)
    {
        var referenceDataService = new Mock<ISearchReferenceDataService>();
        referenceDataService
            .Setup(s => s.GetPersonsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PersonDto>());
        referenceDataService
            .Setup(s => s.GetTagsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TagDto>());

        var normalizer = new Mock<ISearchFilterNormalizer>();
        normalizer
            .Setup(n => n.NormalizeAsync(It.IsAny<FilterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FilterDto f, CancellationToken _) => f);

        return new PhotoService(
            context,
            new Repository<Photo>(provider),
            new Repository<Person>(provider),
            new Repository<Face>(provider),
            new Repository<Storage>(provider),
            new Repository<PersonGroup>(provider),
            _mapper,
            new MemoryCache(new MemoryCacheOptions()),
            currentUser,
            referenceDataService.Object,
            normalizer.Object,
            new StubS3ResourceService(),
            new MinioObjectService(new Mock<IMinioClient>().Object),
            new Mock<IMinioClient>().Object,
            Options.Create(new S3Options { Bucket = "photobank", UrlExpirySeconds = 60 }));
    }

    private static ServiceProvider BuildProvider(string dbName)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<PhotoBankDbContext>(o => o.UseInMemoryDatabase(dbName));
        return services.BuildServiceProvider();
    }

    private sealed class StubS3ResourceService : S3ResourceService
    {
        public StubS3ResourceService()
            : base(new Mock<IMinioClient>().Object)
        {
        }

        protected override Task<string?> GetPresignedUrlAsync(string key)
            => Task.FromResult<string?>(key + "-url");

        protected override Task<byte[]> GetObjectAsync(string key)
            => Task.FromResult(Array.Empty<byte>());
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public TestCurrentUser(
            bool isAdmin,
            IEnumerable<int>? allowedStorageIds,
            IEnumerable<int>? allowedPersonGroupIds,
            IEnumerable<(DateOnly From, DateOnly To)>? dateRanges = null,
            bool canSeeNsfw = true)
        {
            IsAdmin = isAdmin;
            AllowedStorageIds = new HashSet<int>(allowedStorageIds ?? Array.Empty<int>());
            AllowedPersonGroupIds = new HashSet<int>(allowedPersonGroupIds ?? Array.Empty<int>());
            AllowedDateRanges = new List<(DateOnly, DateOnly)>(dateRanges ?? Array.Empty<(DateOnly, DateOnly)>());
            CanSeeNsfw = canSeeNsfw;
        }

        public string UserId => "user";
        public bool IsAdmin { get; }
        public IReadOnlySet<int> AllowedStorageIds { get; }
        public IReadOnlySet<int> AllowedPersonGroupIds { get; }
        public IReadOnlyList<(DateOnly From, DateOnly To)> AllowedDateRanges { get; }
        public bool CanSeeNsfw { get; }
    }
}
