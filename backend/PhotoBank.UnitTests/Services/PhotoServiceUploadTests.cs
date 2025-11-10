using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
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
using PhotoBank.Services.Photos;
using PhotoBank.Services.Photos.Admin;
using PhotoBank.Services.Photos.Faces;
using PhotoBank.Services.Photos.Queries;
using PhotoBank.Services.Photos.Upload;
using PhotoBank.Services.Search;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Abstractions;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.UnitTests.Services
{
    [TestFixture]
    public class PhotoServiceUploadTests
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

        private PhotoService CreateService(PhotoBankDbContext context, IServiceProvider provider)
        {
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

            var minioClient = new Mock<IMinioClient>();
            var mediaUrlResolver = new Mock<IMediaUrlResolver>();
            mediaUrlResolver
                .Setup(r => r.ResolveAsync(
                    It.IsAny<string?>(),
                    It.IsAny<int>(),
                    It.IsAny<MediaUrlContext>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((string? key, int _, MediaUrlContext _, CancellationToken _) =>
                    string.IsNullOrEmpty(key) ? null : $"resolved-{key}");
            var s3Options = new Mock<IOptions<S3Options>>();
            s3Options.Setup(o => o.Value).Returns(new S3Options());

            var photoRepository = new Repository<Photo>(provider);
            var personRepository = new Repository<Person>(provider);
            var faceRepository = new Repository<Face>(provider);
            var storageRepository = new Repository<Storage>(provider);
            var personGroupRepository = new Repository<PersonGroup>(provider);

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
                mediaUrlResolver.Object,
                s3Options.Object);

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
                referenceDataService.Object,
                s3Options.Object);

            var duplicateFinder = new Mock<IPhotoDuplicateFinder>();
            duplicateFinder
                .Setup(f => f.FindDuplicatesAsync(It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<PhotoItemDto>());

            var fileSystem = new FileSystem();

            var nameResolver = new UploadNameResolver();
            var fileSystemStrategy = new FileSystemStorageUploadStrategy(
                fileSystem,
                nameResolver,
                NullLogger<FileSystemStorageUploadStrategy>.Instance);
            var objectStorageStrategy = new ObjectStorageUploadStrategy(
                minioClient.Object,
                s3Options.Object,
                nameResolver,
                NullLogger<ObjectStorageUploadStrategy>.Instance);
            var photoIngestionService = new PhotoIngestionService(
                storageRepository,
                new IStorageUploadStrategy[] { objectStorageStrategy, fileSystemStrategy },
                NullLogger<PhotoIngestionService>.Instance);

            return new PhotoService(
                photoQueryService,
                personDirectoryService,
                personGroupService,
                faceCatalogService,
                duplicateFinder.Object,
                photoIngestionService);
        }

        [Test]
        public async Task UploadPhotosAsync_SavesFilesToStorage()
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var services = new ServiceCollection();
            services.AddDbContext<PhotoBankDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            var provider = services.BuildServiceProvider();
            var context = provider.GetRequiredService<PhotoBankDbContext>();

            var storage = new Storage { Name = "test", Folder = tempFolder };
            context.Storages.Add(storage);
            await context.SaveChangesAsync();

            var service = CreateService(context, provider);

            var bytes = new byte[] { 1, 2, 3, 4 };
            await using var ms = new MemoryStream(bytes);
            IFormFile file = new FormFile(ms, 0, bytes.Length, "file", "test.bin");

            await service.UploadPhotosAsync(new[] { file }, storage.Id, "sub");

            var expectedPath = Path.Combine(tempFolder, "sub", "test.bin");
            System.IO.File.Exists(expectedPath).Should().BeTrue();

            Directory.Delete(tempFolder, true);
        }

        [Test]
        public async Task UploadPhotosAsync_DoesNotSaveDuplicateFiles()
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var services = new ServiceCollection();
            services.AddDbContext<PhotoBankDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            var provider = services.BuildServiceProvider();
            var context = provider.GetRequiredService<PhotoBankDbContext>();

            var storage = new Storage { Name = "test", Folder = tempFolder };
            context.Storages.Add(storage);
            await context.SaveChangesAsync();

            var service = CreateService(context, provider);

            var bytes = new byte[] { 1, 2, 3, 4 };
            await using var ms1 = new MemoryStream(bytes);
            IFormFile file1 = new FormFile(ms1, 0, bytes.Length, "file", "test.bin");
            await service.UploadPhotosAsync(new[] { file1 }, storage.Id, "");

            await using var ms2 = new MemoryStream(bytes);
            IFormFile file2 = new FormFile(ms2, 0, bytes.Length, "file", "test.bin");
            await service.UploadPhotosAsync(new[] { file2 }, storage.Id, "");

            Directory.GetFiles(tempFolder).Should().HaveCount(1);
            var expectedPath = Path.Combine(tempFolder, "test.bin");
            new FileInfo(expectedPath).Length.Should().Be(bytes.Length);

            Directory.Delete(tempFolder, true);
        }

        [Test]
        public async Task UploadPhotosAsync_RenamesFileWhenSizeDiffers()
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var services = new ServiceCollection();
            services.AddDbContext<PhotoBankDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            var provider = services.BuildServiceProvider();
            var context = provider.GetRequiredService<PhotoBankDbContext>();

            var storage = new Storage { Name = "test", Folder = tempFolder };
            context.Storages.Add(storage);
            await context.SaveChangesAsync();

            var service = CreateService(context, provider);

            var bytes1 = new byte[] { 1, 2, 3, 4 };
            await using var ms1 = new MemoryStream(bytes1);
            IFormFile file1 = new FormFile(ms1, 0, bytes1.Length, "file", "test.bin");
            await service.UploadPhotosAsync(new[] { file1 }, storage.Id, "");

            var bytes2 = new byte[] { 5, 6 };
            await using var ms2 = new MemoryStream(bytes2);
            IFormFile file2 = new FormFile(ms2, 0, bytes2.Length, "file", "test.bin");
            await service.UploadPhotosAsync(new[] { file2 }, storage.Id, "");

            Directory.GetFiles(tempFolder).Should().HaveCount(2);
            var originalPath = Path.Combine(tempFolder, "test.bin");
            var renamedPath = Path.Combine(tempFolder, "test_1.bin");
            new FileInfo(originalPath).Length.Should().Be(bytes1.Length);
            new FileInfo(renamedPath).Length.Should().Be(bytes2.Length);

            Directory.Delete(tempFolder, true);
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
}
