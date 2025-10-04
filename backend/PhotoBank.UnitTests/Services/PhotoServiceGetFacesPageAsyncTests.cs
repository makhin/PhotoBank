using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Minio;
using Minio.DataModel.Args;
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
using PhotoBank.Services.Search;
using PhotoBank.ViewModel.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
            Storage = storage,
            StorageId = storage.Id,
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
            RelativePath = "faces",
            Captions = new List<Caption>(),
            PhotoTags = new List<PhotoTag>(),
            PhotoCategories = new List<PhotoCategory>(),
            ObjectProperties = new List<ObjectProperty>(),
            Faces = new List<Face>()
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

        var minioClient = new Mock<IMinioClient>();
        minioClient
            .Setup(m => m.PresignedGetObjectAsync(It.IsAny<PresignedGetObjectArgs>()))
            .ReturnsAsync("https://example.com/face.jpg");

        var service = CreateService(dbName, minioClient.Object);

        // Act
        var result = await service.GetFacesPageAsync(1, 10);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items.Should().AllSatisfy(dto => dto.ImageUrl.Should().Be("https://example.com/face.jpg"));
    }

    private PhotoService CreateService(string dbName, IMinioClient minioClient)
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
            NullLogger<PhotoService>.Instance,
            new DummyCurrentUser(),
            referenceDataService.Object,
            normalizer.Object,
            minioClient,
            Options.Create(new S3Options { Bucket = "bucket", UrlExpirySeconds = 60 }));
    }
}
