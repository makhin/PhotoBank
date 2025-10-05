using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
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
using PhotoBank.Services.Photos;
using PhotoBank.Services.Photos.Admin;
using PhotoBank.Services.Photos.Faces;
using PhotoBank.Services.Photos.Queries;
using PhotoBank.Services.Search;
using System;
using System.Threading;
using System.Threading.Tasks;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.UnitTests;

[TestFixture]
public class PersonGroupServiceTests
{
    private ServiceProvider _provider = null!;
    private IPhotoService _service = null!;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<PhotoBankDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddMemoryCache();
        services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
        services.AddScoped<ICurrentUser, DummyCurrentUser>();
        services.AddScoped<ISearchReferenceDataService, SearchReferenceDataService>();
        _provider = services.BuildServiceProvider();

        var db = _provider.GetRequiredService<PhotoBankDbContext>();
        db.Persons.Add(new Person { Id = 1, Name = "John" });
        db.PersonGroups.Add(new PersonGroup { Id = 1, Name = "Family" });
        db.SaveChanges();

        var normalizer = new Mock<ISearchFilterNormalizer>();
        normalizer
            .Setup(n => n.NormalizeAsync(It.IsAny<FilterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FilterDto f, CancellationToken _) => f);

        var memoryCache = _provider.GetRequiredService<IMemoryCache>();
        var mapper = _provider.GetRequiredService<IMapper>();
        var currentUser = _provider.GetRequiredService<ICurrentUser>();
        var referenceDataService = _provider.GetRequiredService<ISearchReferenceDataService>();
        var photoRepository = _provider.GetRequiredService<IRepository<Photo>>();
        var personRepository = _provider.GetRequiredService<IRepository<Person>>();
        var faceRepository = _provider.GetRequiredService<IRepository<Face>>();
        var storageRepository = _provider.GetRequiredService<IRepository<Storage>>();
        var personGroupRepository = _provider.GetRequiredService<IRepository<PersonGroup>>();
        var minioClient = new Mock<IMinioClient>();
        var s3Options = Options.Create(new S3Options());

        var photoFilterSpecification = new PhotoFilterSpecification(db);

        var photoQueryService = new PhotoQueryService(
            db,
            photoRepository,
            storageRepository,
            mapper,
            memoryCache,
            NullLogger<PhotoQueryService>.Instance,
            currentUser,
            referenceDataService,
            normalizer.Object,
            photoFilterSpecification,
            minioClient.Object,
            s3Options);

        var personDirectoryService = new PersonDirectoryService(
            personRepository,
            mapper,
            referenceDataService,
            NullLogger<PersonDirectoryService>.Instance);

        var personGroupService = new PersonGroupService(
            db,
            personGroupRepository,
            mapper,
            memoryCache,
            NullLogger<PersonGroupService>.Instance);

        var faceCatalogService = new FaceCatalogService(
            faceRepository,
            mapper,
            minioClient.Object,
            NullLogger<FaceCatalogService>.Instance,
            s3Options);

        var duplicateFinder = new Mock<IPhotoDuplicateFinder>();
        duplicateFinder
            .Setup(f => f.FindDuplicatesAsync(It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PhotoItemDto>());

        var ingestionService = new Mock<IPhotoIngestionService>();

        _service = new PhotoService(
            photoQueryService,
            personDirectoryService,
            personGroupService,
            faceCatalogService,
            duplicateFinder.Object,
            ingestionService.Object);
    }

    [TearDown]
    public void TearDown() => _provider.Dispose();

    [Test]
    public async Task AddPersonToGroupAsync_AddsLink()
    {
        await _service.AddPersonToGroupAsync(1, 1);
        var db = _provider.GetRequiredService<PhotoBankDbContext>();
        var person = await db.Persons.Include(p => p.PersonGroups).SingleAsync(p => p.Id == 1);
        person.PersonGroups.Should().ContainSingle(g => g.Id == 1);
    }

    [Test]
    public async Task RemovePersonFromGroupAsync_RemovesLink()
    {
        await _service.AddPersonToGroupAsync(1, 1);
        await _service.RemovePersonFromGroupAsync(1, 1);
        var db = _provider.GetRequiredService<PhotoBankDbContext>();
        var person = await db.Persons.Include(p => p.PersonGroups).SingleAsync(p => p.Id == 1);
        person.PersonGroups.Should().BeEmpty();
    }

    [Test]
    public async Task GetAllPersonGroupsAsync_ReturnsGroup()
    {
        var groups = await _service.GetAllPersonGroupsAsync();
        groups.Should().ContainSingle(g => g.Name == "Family");
    }

    [Test]
    public async Task AddPersonToGroupAsync_InvalidatesPersonGroupsCache()
    {
        var cache = _provider.GetRequiredService<IMemoryCache>();

        await _service.GetAllPersonGroupsAsync();
        cache.TryGetValue(CacheKeys.PersonGroups, out _).Should().BeTrue();

        await _service.AddPersonToGroupAsync(1, 1);

        cache.TryGetValue(CacheKeys.PersonGroups, out _).Should().BeFalse();
    }

    [Test]
    public async Task RemovePersonFromGroupAsync_InvalidatesPersonGroupsCache()
    {
        var cache = _provider.GetRequiredService<IMemoryCache>();

        await _service.AddPersonToGroupAsync(1, 1);
        await _service.GetAllPersonGroupsAsync();
        cache.TryGetValue(CacheKeys.PersonGroups, out _).Should().BeTrue();

        await _service.RemovePersonFromGroupAsync(1, 1);

        cache.TryGetValue(CacheKeys.PersonGroups, out _).Should().BeFalse();
    }
}

