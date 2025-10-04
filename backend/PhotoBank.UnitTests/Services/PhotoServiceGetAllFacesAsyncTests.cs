using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minio;
using Moq;
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
public class PhotoServiceGetAllFacesAsyncTests
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
    public async Task GetAllFacesAsync_FillsImageUrls()
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
            S3Key_Preview = "preview",
            S3Key_Thumbnail = "thumbnail",
            ImageHash = "hash",
            Faces = new List<Face>()
        };
        context.Photos.Add(photo);
        await context.SaveChangesAsync();

        var face = new Face
        {
            Photo = photo,
            PhotoId = photo.Id,
            Rectangle = new Point(0, 0),
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

        var minioClient = new Mock<IMinioClient>();
        minioClient
            .Setup(m => m.PresignedGetObjectAsync(It.IsAny<PresignedGetObjectArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://example.com/face.jpg");

        var service = CreateService(dbName, minioClient.Object);

        // Act
        var result = await service.GetAllFacesAsync();

        // Assert
        result.Should().BeAssignableTo<IReadOnlyCollection<FaceDto>>();
        result.Should().ContainSingle();
        result.Should().AllSatisfy(dto => dto.ImageUrl.Should().Be("https://example.com/face.jpg"));
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
            new DummyCurrentUser(),
            referenceDataService.Object,
            normalizer.Object,
            minioClient,
            Options.Create(new S3Options { Bucket = "bucket", UrlExpirySeconds = 60 }));
    }
}
