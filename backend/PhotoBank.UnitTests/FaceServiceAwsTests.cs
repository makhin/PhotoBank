using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services;
using PhotoBank.UnitTests.Infrastructure.FaceRecognition.Aws;
using DbFace = PhotoBank.DbContext.Models.Face;
using DbPhoto = PhotoBank.DbContext.Models.Photo;
using DbStorage = PhotoBank.DbContext.Models.Storage;
using DbCaption = PhotoBank.DbContext.Models.Caption;
using DbPhotoTag = PhotoBank.DbContext.Models.PhotoTag;
using DbPhotoCategory = PhotoBank.DbContext.Models.PhotoCategory;
using DbObjectProperty = PhotoBank.DbContext.Models.ObjectProperty;
using DbFile = PhotoBank.DbContext.Models.File;
using DbPerson = PhotoBank.DbContext.Models.Person;

namespace PhotoBank.UnitTests;

[TestFixture]
public class FaceServiceAwsTests
{
    private const string PersonGroupId = "my-cicrle-person-group";

    private Mock<AmazonRekognitionClient> _rekognitionClient = null!;
    private PhotoBankDbContext _dbContext = null!;
    private ServiceProvider _serviceProvider = null!;
    private Repository<DbFace> _faceRepository = null!;
    private Repository<DbPerson> _personRepository = null!;
    private Mock<IFaceStorageService> _storage = null!;
    private Mock<ILogger<FaceService>> _logger = null!;
    private FaceServiceAws _service = null!;

    [SetUp]
    public void SetUp()
    {
        _rekognitionClient = RekognitionClientMockFactory.Create();

        _dbContext = TestDbFactory.CreateInMemory();
        var services = new ServiceCollection();
        services.AddSingleton(_dbContext);
        _serviceProvider = services.BuildServiceProvider();

        _faceRepository = new Repository<DbFace>(_serviceProvider);
        _personRepository = new Repository<DbPerson>(_serviceProvider);
        _storage = new Mock<IFaceStorageService>();
        _logger = new Mock<ILogger<FaceService>>();

        _service = new FaceServiceAws(
            _rekognitionClient.Object,
            _faceRepository,
            _personRepository,
            _storage.Object,
            _logger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
        _serviceProvider.Dispose();
    }

    [Test]
    public async Task SyncPersonsAsync_ShouldCreateMissingCollectionAndSyncUsers()
    {
        _dbContext.Persons.AddRange(
            new DbPerson { Id = 1, Name = "Alice" },
            new DbPerson { Id = 2, Name = "Bob" });
        await _dbContext.SaveChangesAsync();

        _rekognitionClient
            .Setup(c => c.ListCollectionsAsync(
                It.Is<ListCollectionsRequest>(r => r.MaxResults == 1000),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RekognitionResponseBuilder.Collections("other-collection"))
            .Verifiable();

        _rekognitionClient
            .Setup(c => c.CreateCollectionAsync(
                It.Is<CreateCollectionRequest>(r => r.CollectionId == PersonGroupId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RekognitionResponseBuilder.CollectionCreated())
            .Verifiable();

        _rekognitionClient
            .Setup(c => c.ListUsersAsync(
                It.Is<ListUsersRequest>(r => r.CollectionId == PersonGroupId && r.MaxResults == 500),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RekognitionResponseBuilder.Users("2", "3"));

        _rekognitionClient
            .Setup(c => c.CreateUserAsync(
                It.Is<CreateUserRequest>(r => r.CollectionId == PersonGroupId && r.UserId == "1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RekognitionResponseBuilder.UserCreated())
            .Verifiable();

        _rekognitionClient
            .Setup(c => c.DeleteUserAsync(
                It.Is<DeleteUserRequest>(r => r.CollectionId == PersonGroupId && r.UserId == "3"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RekognitionResponseBuilder.UserDeleted())
            .Verifiable();

        await _service.SyncPersonsAsync();

        _rekognitionClient.VerifyAll();
        _rekognitionClient.Verify(c => c.ListUsersAsync(
            It.Is<ListUsersRequest>(r => r.CollectionId == PersonGroupId && r.MaxResults == 500),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SyncFacesToPersonAsync_ShouldIndexAndAssociateNewFaces()
    {
        var face = CreateFace(
            faceId: 5,
            personId: 10,
            key: "key",
            etag: "etag",
            sha: "sha",
            externalGuid: Guid.Empty,
            externalId: null,
            provider: null);

        _dbContext.Add(face);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        _storage
            .Setup(s => s.OpenReadStreamAsync(
                It.Is<DbFace>(f => f.Id == face.Id && f.S3Key_Image == face.S3Key_Image),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(new byte[] { 1, 2, 3 }));

        _rekognitionClient
            .Setup(c => c.ListFacesAsync(
                It.Is<ListFacesRequest>(r => r.CollectionId == PersonGroupId && r.UserId == face.PersonId.ToString()),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RekognitionResponseBuilder.Faces());

        var indexedFaceId = Guid.NewGuid().ToString();

        _rekognitionClient
            .Setup(c => c.IndexFacesAsync(
                It.Is<IndexFacesRequest>(r =>
                    r.CollectionId == PersonGroupId &&
                    r.DetectionAttributes.Contains("ALL") &&
                    r.MaxFaces == 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RekognitionResponseBuilder.IndexedFaces(indexedFaceId));

        _rekognitionClient
            .Setup(c => c.AssociateFacesAsync(
                It.Is<AssociateFacesRequest>(r =>
                    r.CollectionId == PersonGroupId &&
                    r.UserId == face.PersonId.ToString() &&
                    r.FaceIds.Single() == indexedFaceId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RekognitionResponseBuilder.AssociatedFaces(indexedFaceId));

        await _service.SyncFacesToPersonAsync();

        _rekognitionClient.Verify(c => c.IndexFacesAsync(
            It.IsAny<IndexFacesRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _rekognitionClient.Verify(c => c.AssociateFacesAsync(
            It.IsAny<AssociateFacesRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);

        var updatedFace = await _dbContext.Faces.AsNoTracking().SingleAsync(f => f.Id == face.Id);
        updatedFace.Provider.Should().Be("Aws");
        updatedFace.ExternalId.Should().Be(indexedFaceId);
        updatedFace.ExternalGuid.Should().Be(Guid.Parse(indexedFaceId));
    }

    [Test]
    public async Task SyncFacesToPersonAsync_WhenFaceAlreadyIndexed_ShouldSkipIndexing()
    {
        var existingGuid = Guid.NewGuid();
        var face = CreateFace(
            faceId: 7,
            personId: 42,
            key: "key",
            etag: "etag",
            sha: "sha",
            externalGuid: existingGuid,
            externalId: existingGuid.ToString(),
            provider: "Aws");

        _dbContext.Add(face);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        _rekognitionClient
            .Setup(c => c.ListFacesAsync(
                It.Is<ListFacesRequest>(r => r.CollectionId == PersonGroupId && r.UserId == face.PersonId.ToString()),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RekognitionResponseBuilder.Faces(existingGuid.ToString()));

        await _service.SyncFacesToPersonAsync();

        _rekognitionClient.Verify(c => c.IndexFacesAsync(
            It.IsAny<IndexFacesRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DetectFacesAsync_WhenNoFacesFound_ShouldReturnEmptyListAndUseAllAttributes()
    {
        DetectFacesRequest? capturedRequest = null;

        _rekognitionClient
            .Setup(c => c.DetectFacesAsync(
                It.IsAny<DetectFacesRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback((DetectFacesRequest request, CancellationToken _) => capturedRequest = request)
            .ReturnsAsync(new DetectFacesResponse
            {
                FaceDetails = new List<FaceDetail>()
            });

        var result = await _service.DetectFacesAsync(new byte[] { 1, 2, 3 });

        result.Should().NotBeNull().And.BeEmpty();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Attributes.Should().Contain("ALL");
        capturedRequest.Image.Bytes.Should().BeOfType<MemoryStream>();
    }

    [Test]
    public async Task DetectFacesAsync_WhenFacesFound_ShouldReturnDetails()
    {
        var expectedFaces = new List<FaceDetail> { new() };

        _rekognitionClient
            .Setup(c => c.DetectFacesAsync(
                It.IsAny<DetectFacesRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RekognitionResponseBuilder.DetectedFaces((IList<FaceDetail>)expectedFaces));

        var result = await _service.DetectFacesAsync(new byte[] { 4, 5 });

        result.Should().BeEquivalentTo(expectedFaces);
    }

    [Test]
    public async Task SearchUsersByImageAsync_WhenMatchesFound_ShouldReturnMatchesAndUseCollection()
    {
        SearchUsersByImageRequest? capturedRequest = null;
        var matches = new List<UserMatch> { new() };

        _rekognitionClient
            .Setup(c => c.SearchUsersByImageAsync(
                It.IsAny<SearchUsersByImageRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback((SearchUsersByImageRequest request, CancellationToken _) => capturedRequest = request)
            .ReturnsAsync(RekognitionResponseBuilder.UserMatches(matches));

        var result = await _service.SearchUsersByImageAsync(new byte[] { 9, 9 });

        result.Should().BeSameAs(matches);
        capturedRequest.Should().NotBeNull();
        capturedRequest!.CollectionId.Should().Be(PersonGroupId);
        capturedRequest.Image.Bytes.Should().BeOfType<MemoryStream>();
    }

    [Test]
    public async Task SearchUsersByImageAsync_WhenNoMatchesFound_ShouldReturnEmptyList()
    {
        _rekognitionClient
            .Setup(c => c.SearchUsersByImageAsync(
                It.IsAny<SearchUsersByImageRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RekognitionResponseBuilder.UserMatches());

        var result = await _service.SearchUsersByImageAsync(new byte[] { 2, 1 });

        result.Should().NotBeNull().And.BeEmpty();
    }

    private static DbFace CreateFace(
        int faceId,
        int personId,
        string key,
        string etag,
        string sha,
        Guid externalGuid,
        string? externalId,
        string? provider)
    {
        var storage = new DbStorage
        {
            Id = 1000 + faceId,
            Name = $"storage-{faceId}",
            Folder = "folder",
            Photos = new List<DbPhoto>()
        };

        var photo = new DbPhoto
        {
            Id = 2000 + faceId,
            StorageId = storage.Id,
            Storage = storage,
            Name = $"photo-{faceId}",
            AccentColor = "ffffff",
            DominantColorBackground = "bg",
            DominantColorForeground = "fg",
            DominantColors = "colors",
            S3Key_Preview = $"preview-{faceId}",
            S3ETag_Preview = $"etagp-{faceId}",
            Sha256_Preview = $"shap-{faceId}",
            S3Key_Thumbnail = $"thumb-{faceId}",
            S3ETag_Thumbnail = $"etagt-{faceId}",
            Sha256_Thumbnail = $"shat-{faceId}",
            Captions = new List<DbCaption>(),
            PhotoTags = new List<DbPhotoTag>(),
            PhotoCategories = new List<DbPhotoCategory>(),
            ObjectProperties = new List<DbObjectProperty>(),
            Faces = new List<DbFace>(),
            Files = new List<DbFile>(),
            ImageHash = $"hash-{faceId}",
            RelativePath = $"path-{faceId}"
        };

        storage.Photos = new List<DbPhoto> { photo };

        var face = new DbFace
        {
            Id = faceId,
            PersonId = personId,
            PhotoId = photo.Id,
            Photo = photo,
            S3Key_Image = key,
            S3ETag_Image = etag,
            Sha256_Image = sha,
            FaceAttributes = "{}",
            Rectangle = new NetTopologySuite.Geometries.Point(0, 0),
            ExternalGuid = externalGuid,
            ExternalId = externalId,
            Provider = provider
        };

        photo.Faces.Add(face);

        return face;
    }
}
