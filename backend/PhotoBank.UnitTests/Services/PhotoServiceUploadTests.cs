using System;
using System.IO;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Minio;
using NUnit.Framework;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services;
using PhotoBank.Services.Api;
using PhotoBank.AccessControl;

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

            var service = new PhotoService(
                context,
                new Repository<Photo>(provider),
                new Repository<Person>(provider),
                new Repository<Face>(provider),
                new Repository<Storage>(provider),
                new Repository<Tag>(provider),
                new Repository<PersonGroup>(provider),
                new Repository<PersonFace>(provider),
                _mapper,
                new MemoryCache(new MemoryCacheOptions()),
                new DummyCurrentUser(),
                new Mock<IMinioClient>().Object);

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

            var service = new PhotoService(
                context,
                new Repository<Photo>(provider),
                new Repository<Person>(provider),
                new Repository<Face>(provider),
                new Repository<Storage>(provider),
                new Repository<Tag>(provider),
                new Repository<PersonGroup>(provider),
                new Repository<PersonFace>(provider),
                _mapper,
                new MemoryCache(new MemoryCacheOptions()),
                new DummyCurrentUser(),
                new Mock<IMinioClient>().Object);

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

            var service = new PhotoService(
                context,
                new Repository<Photo>(provider),
                new Repository<Person>(provider),
                new Repository<Face>(provider),
                new Repository<Storage>(provider),
                new Repository<Tag>(provider),
                new Repository<PersonGroup>(provider),
                new Repository<PersonFace>(provider),
                _mapper,
                new MemoryCache(new MemoryCacheOptions()),
                new DummyCurrentUser(),
                new Mock<IMinioClient>().Object);

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
    }
}
