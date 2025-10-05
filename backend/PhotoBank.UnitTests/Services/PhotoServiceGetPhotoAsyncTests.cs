using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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
using PhotoBank.ViewModel.Dto;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoBank.UnitTests.Services
{
    [TestFixture]
    public class PhotoServiceGetPhotoAsyncTests
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
        public async Task GetPhotoAsync_NonAdminUser_ReturnsPhotoWhenAclAllows()
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
                Folder = "folder",
                Photos = new List<Photo>()
            };
            context.Storages.Add(storage);
            await context.SaveChangesAsync();

            var photo = new Photo
            {
                Name = "photo",
                Storage = storage,
                StorageId = storage.Id,
                S3Key_Preview = "preview",
                S3ETag_Preview = "preview-etag",
                Sha256_Preview = "preview-hash",
                S3Key_Thumbnail = "thumbnail",
                S3ETag_Thumbnail = "thumb-etag",
                Sha256_Thumbnail = "thumb-hash",
                AccentColor = "000000",
                DominantColorBackground = "000000",
                DominantColorForeground = "FFFFFF",
                DominantColors = "[]",
                Faces = new List<Face>(),
                Captions = new List<Caption>(),
                PhotoTags = new List<PhotoTag>(),
                PhotoCategories = new List<PhotoCategory>(),
                ObjectProperties = new List<ObjectProperty>(),
                Files = new List<File>(),
                TakenDate = DateTime.UtcNow,
                IsAdultContent = false,
                IsRacyContent = false,
                RelativePath = "path",
                ImageHash = "hash"
            };
            context.Photos.Add(photo);
            await context.SaveChangesAsync();

            var currentUser = new TestCurrentUser(storage.Id);

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

            var mediaUrlResolver = new Mock<IMediaUrlResolver>();
            mediaUrlResolver
                .Setup(r => r.ResolveAsync(
                    It.IsAny<string?>(),
                    It.IsAny<int>(),
                    It.IsAny<MediaUrlContext>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync("https://example.com/object");

            var filterNormalizer = new Mock<ISearchFilterNormalizer>();

            var photoRepository = new Repository<Photo>(provider);
            var personRepository = new Repository<Person>(provider);
            var faceRepository = new Repository<Face>(provider);
            var personGroupRepository = new Repository<PersonGroup>(provider);
            var s3Options = Options.Create(new S3Options());

            var photoFilterSpecification = new PhotoFilterSpecification(context);

            var photoQueryService = new PhotoQueryService(
                context,
                photoRepository,
                _mapper,
                currentUser,
                referenceDataService.Object,
                filterNormalizer.Object,
                photoFilterSpecification,
                mediaUrlResolver.Object,
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
                mediaUrlResolver.Object,
                s3Options);

            var duplicateFinder = new Mock<IPhotoDuplicateFinder>();
            duplicateFinder
                .Setup(f => f.FindDuplicatesAsync(It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<PhotoItemDto>());

            var ingestionService = new Mock<IPhotoIngestionService>();

            var service = new PhotoService(
                photoQueryService,
                personDirectoryService,
                personGroupService,
                faceCatalogService,
                duplicateFinder.Object,
                ingestionService.Object);

            // Act
            var result = await service.GetPhotoAsync(photo.Id);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(photo.Id);
            result.PreviewUrl.Should().Be("https://example.com/object");
        }

        private sealed class TestCurrentUser : ICurrentUser
        {
            public TestCurrentUser(int storageId)
            {
                AllowedStorageIds = new HashSet<int> { storageId };
                AllowedPersonGroupIds = new HashSet<int>();
                AllowedDateRanges = Array.Empty<(DateOnly From, DateOnly To)>();
            }

            public string UserId => "user";

            public bool IsAdmin => false;

            public IReadOnlySet<int> AllowedStorageIds { get; }

            public IReadOnlySet<int> AllowedPersonGroupIds { get; }

            public IReadOnlyList<(DateOnly From, DateOnly To)> AllowedDateRanges { get; }

            public bool CanSeeNsfw => true;
        }
    }
}
