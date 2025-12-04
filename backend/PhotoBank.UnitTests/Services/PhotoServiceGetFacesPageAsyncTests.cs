using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NUnit.Framework;
using PhotoBank.AccessControl;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services;
using PhotoBank.Services.Api;
using PhotoBank.Services.Internal;
using PhotoBank.Services.Photos;
using PhotoBank.Services.Photos.Admin;
using PhotoBank.Services.Photos.Faces;
using System.Threading;
using PhotoBank.Services.Photos.Queries;
using PhotoBank.Services.Search;
using PhotoBank.ViewModel.Dto;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhotoBank.UnitTests.Services;

[TestFixture]
public class PhotoServiceGetFacesPageAsyncTests
{
    private IMapper _mapper = null!;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddProfile(new MappingProfile()));

        var provider = services.BuildServiceProvider();
        _mapper = provider.GetRequiredService<IMapper>();
    }

    [Test]
    public async Task GetFacesPageAsync_FillsImageUrls()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<PhotoBankDbContext>(o => o.UseInMemoryDatabase(dbName));
        var provider = services.BuildServiceProvider();
        var context = provider.GetRequiredService<PhotoBankDbContext>();

        var storage = new Storage
        {
            Name = "storage",
            Folder = "/"
        };
        context.Storages.Add(storage);
        await context.SaveChangesAsync();

        var photo = new Photo
        {
            Name = "photo.jpg",
            AccentColor = "000000",
            DominantColorBackground = "000000",
            DominantColorForeground = "000000",
            DominantColors = "000000",
            S3Key_Preview = "preview",
            S3ETag_Preview = "etag-preview",
            Sha256_Preview = "sha-preview",
            S3Key_Thumbnail = "thumbnail",
            S3ETag_Thumbnail = "etag-thumbnail",
            Sha256_Thumbnail = "sha-thumbnail",
            ImageHash = "hash",
            Captions = new List<Caption>(),
            PhotoTags = new List<PhotoTag>(),
            PhotoCategories = new List<PhotoCategory>(),
            ObjectProperties = new List<ObjectProperty>(),
            Faces = new List<Face>(),
            Files = new List<File>
            {
                new()
                {
                    StorageId = storage.Id,
                    Storage = storage,
                    RelativePath = "faces",
                    Name = "photo.jpg"
                }
            }
        };
        context.Photos.Add(photo);
        await context.SaveChangesAsync();

        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory();
        var face = new Face
        {
            Photo = photo,
            PhotoId = photo.Id,
            Rectangle = geometryFactory.CreatePolygon(
                new[]
                {
                    new Coordinate(0, 0),
                    new Coordinate(10, 0),
                    new Coordinate(10, 10),
                    new Coordinate(0, 10),
                    new Coordinate(0, 0),
                }),
            S3Key_Image = "face-key",
            S3ETag_Image = "etag",
            Sha256_Image = "sha",
            FaceAttributes = "{}",
            IdentityStatus = IdentityStatus.Identified,
            IdentifiedWithConfidence = 1,
            ExternalGuid = Guid.NewGuid()
        };
        context.Faces.Add(face);
        await context.SaveChangesAsync();

        var savedFace = await context.Faces.AsNoTracking().FirstAsync();
        savedFace.Rectangle.Coordinates.Length.Should().BeGreaterThan(3);

        var mediaUrlResolver = new Mock<IMediaUrlResolver>();
        mediaUrlResolver
            .Setup(r => r.ResolveAsync(
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<MediaUrlContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://example.com/face.jpg");

        var service = CreateService(dbName, mediaUrlResolver.Object);

        // Act
        var result = await service.GetFacesPageAsync(1, 10);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items.Should().AllSatisfy(dto => dto.ImageUrl.Should().Be("https://example.com/face.jpg"));
    }

    private PhotoService CreateService(string dbName, IMediaUrlResolver mediaUrlResolver)
    {
        var services = new ServiceCollection();
        services.AddDbContext<PhotoBankDbContext>(o => o.UseInMemoryDatabase(dbName));
        var provider = services.BuildServiceProvider();
        var context = provider.GetRequiredService<PhotoBankDbContext>();

        var referenceDataService = new Mock<ISearchReferenceDataService>();
        referenceDataService
            .Setup(s => s.GetPersonsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PersonDto>());
        referenceDataService
            .Setup(s => s.GetTagsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TagDto>());
        referenceDataService
            .Setup(s => s.GetStoragesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StorageDto>());
        referenceDataService
            .Setup(s => s.GetPathsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PathDto>());
        referenceDataService
            .Setup(s => s.GetPersonGroupsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PersonGroupDto>());

        var normalizer = new Mock<ISearchFilterNormalizer>();
        normalizer
            .Setup(n => n.NormalizeAsync(It.IsAny<FilterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FilterDto f, CancellationToken _) => f);

        var photoRepository = new Repository<Photo>(provider);
        var personRepository = new Repository<Person>(provider);
        var faceRepository = new Repository<Face>(provider);
        var personGroupRepository = new Repository<PersonGroup>(provider);
        var s3Options = Options.Create(new S3Options { Bucket = "bucket", UrlExpirySeconds = 60 });

        var photoFilterSpecification = new PhotoFilterSpecification(context);
        var currentUserAccessor = new TestCurrentUserAccessor(new DummyCurrentUser());

        var photoQueryService = new PhotoQueryService(
            context,
            photoRepository,
            _mapper,
            currentUserAccessor,
            referenceDataService.Object,
            normalizer.Object,
            photoFilterSpecification,
            mediaUrlResolver,
            s3Options);

        var personDirectoryService = new PersonDirectoryService(
            personRepository,
            _mapper,
            referenceDataService.Object,
            NullLogger<PersonDirectoryService>.Instance);

        var personGroupService = new PersonGroupService(
            context,
            personGroupRepository,
            _mapper,
            referenceDataService.Object,
            NullLogger<PersonGroupService>.Instance);

        var faceCatalogService = new FaceCatalogService(
            faceRepository,
            _mapper,
            mediaUrlResolver,
            referenceDataService.Object,
            s3Options);

        var duplicateFinder = new Mock<IPhotoDuplicateFinder>();
        duplicateFinder
            .Setup(f => f.FindDuplicatesAsync(It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PhotoItemDto>());

        var ingestionService = new Mock<IPhotoIngestionService>();

        return new PhotoService(
            photoQueryService,
            personDirectoryService,
            personGroupService,
            faceCatalogService,
            duplicateFinder.Object,
            ingestionService.Object);
    }

    private sealed class TestCurrentUserAccessor : ICurrentUserAccessor
    {
        private readonly ICurrentUser _user;

        public TestCurrentUserAccessor(ICurrentUser user)
        {
            _user = user;
        }

        public ValueTask<ICurrentUser> GetCurrentUserAsync(CancellationToken ct = default)
            => ValueTask.FromResult(_user);

        public ICurrentUser CurrentUser => _user;
    }
}
