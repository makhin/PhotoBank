using AutoMapper;
using FizzWare.NBuilder;
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

namespace PhotoBank.UnitTests.Services
{
    [TestFixture]
    public class PhotoServiceGetAllPhotosAsyncTests
    {
        private IMapper _mapper;

        private static Face CreateFace(Photo photo, Person person)
        {
            return new Face
            {
                Photo = photo,
                PhotoId = photo.Id,
                Person = person,
                PersonId = person.Id,
                Rectangle = new Point(0, 0),
                S3Key_Image = "image",
                S3ETag_Image = "etag",
                Sha256_Image = "hash",
                FaceAttributes = "{}",
                IdentityStatus = IdentityStatus.Identified
            };
        }

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile(new MappingProfile());
            });

            var provider = services.BuildServiceProvider();
            _mapper = provider.GetRequiredService<IMapper>();
        }

        private PhotoService CreateService(string dbName, Mock<ISearchFilterNormalizer>? normalizerMock = null)
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

            var createdMock = normalizerMock is null;
            normalizerMock ??= new Mock<ISearchFilterNormalizer>();
            if (createdMock)
            {
                normalizerMock
                    .Setup(n => n.NormalizeAsync(It.IsAny<FilterDto>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((FilterDto f, CancellationToken _) => f);
            }

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
                normalizerMock.Object,
                new Mock<IMinioClient>().Object,
                new Mock<IOptions<S3Options>>().Object);
        }

        [Test]
        public async Task GetAllPhotosAsync_NormalizesFilter()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            services.AddDbContext<PhotoBankDbContext>(o => o.UseInMemoryDatabase(dbName));
            var provider = services.BuildServiceProvider();
            _ = provider.GetRequiredService<PhotoBankDbContext>();

            var filter = new FilterDto();
            var normalizer = new Mock<ISearchFilterNormalizer>();
            normalizer
                .Setup(n => n.NormalizeAsync(filter, It.IsAny<CancellationToken>()))
                .ReturnsAsync(filter);

            var service = CreateService(dbName, normalizer);

            // Act
            await service.GetAllPhotosAsync(filter, CancellationToken.None);

            // Assert
            normalizer.Verify(n => n.NormalizeAsync(filter, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetAllPhotosAsync_NoFilters_ReturnsAllPhotos()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            services.AddDbContext<PhotoBankDbContext>(o => o.UseInMemoryDatabase(dbName));
            var provider = services.BuildServiceProvider();
            var context = provider.GetRequiredService<PhotoBankDbContext>();

            var storage = Builder<Storage>.CreateNew().With(s => s.Name = "s1").Build();
            context.Storages.Add(storage);
            await context.SaveChangesAsync();

            var photos = Builder<Photo>.CreateListOfSize(2)
                .All()
                .With(p => p.Location = new Point(0, 0))
                .With(p => p.S3Key_Thumbnail = "thumb")
                .With(p => p.Storage = storage)
                .With(p => p.StorageId = storage.Id)
                .With(p => p.Name = Guid.NewGuid().ToString())
                .Build();
            context.Photos.AddRange(photos);
            await context.SaveChangesAsync();

            var service = CreateService(dbName);
            var filter = new FilterDto();

            // Act
            var result = await service.GetAllPhotosAsync(filter);

            // Assert
            result.TotalCount.Should().Be(2);
            result.Items.Should().HaveCount(2);
        }

        [Test]
        public async Task GetAllPhotosAsync_FilterByIsBW_ReturnsOnlyBW()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            services.AddDbContext<PhotoBankDbContext>(o => o.UseInMemoryDatabase(dbName));
            var provider = services.BuildServiceProvider();
            var context = provider.GetRequiredService<PhotoBankDbContext>();

            var storage = Builder<Storage>.CreateNew().With(s => s.Name = "s1").Build();
            context.Storages.Add(storage);
            await context.SaveChangesAsync();

            var bwPhoto = Builder<Photo>.CreateNew()
                .With(p=> p.Id = 1)
                .With(p => p.IsBW = true)
                .With(p => p.Location = new Point(0, 0))
                .With(p => p.S3Key_Thumbnail = "thumb")
                .With(p => p.Storage = storage)
                .With(p => p.StorageId = storage.Id)
                .With(p => p.Name = "bw")
                .Build();
            var colorPhoto = Builder<Photo>.CreateNew()
                .With(p => p.Id = 2)
                .With(p => p.IsBW = false)
                .With(p => p.Location = new Point(0, 0))
                .With(p => p.S3Key_Thumbnail = "thumb")
                .With(p => p.Storage = storage)
                .With(p => p.StorageId = storage.Id)
                .With(p => p.Name = "color")
                .Build();
            context.Photos.AddRange(bwPhoto, colorPhoto);
            await context.SaveChangesAsync();

            var service = CreateService(dbName);
            var filter = new FilterDto { IsBW = true };

            // Act
            var result = await service.GetAllPhotosAsync(filter);

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items.Should().ContainSingle(p => p.Name == "bw");
        }

        [Test]
        public async Task GetAllPhotosAsync_FilterByTag_ReturnsMatchingPhotos()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            services.AddDbContext<PhotoBankDbContext>(o => o.UseInMemoryDatabase(dbName));
            var provider = services.BuildServiceProvider();
            var context = provider.GetRequiredService<PhotoBankDbContext>();

            var storage = Builder<Storage>.CreateNew().With(s => s.Name = "s1").Build();
            context.Storages.Add(storage);
            var tag = Builder<Tag>.CreateNew().With(t => t.Name = "Tag1").Build();
            context.Tags.Add(tag);
            await context.SaveChangesAsync();

            var photoWithTag = Builder<Photo>.CreateNew()
                .With(p => p.Id = 1)
                .With(p => p.Location = new Point(0, 0))
                .With(p => p.S3Key_Thumbnail = "thumb")
                .With(p => p.Storage = storage)
                .With(p => p.StorageId = storage.Id)
                .With(p => p.Name = "withTag")
                .Build();
            photoWithTag.PhotoTags = new List<PhotoTag>
            {
                new PhotoTag { Photo = photoWithTag, Tag = tag }
            };
            var photoWithoutTag = Builder<Photo>.CreateNew()
                .With(p => p.Id = 2)
                .With(p => p.Location = new Point(0, 0))
                .With(p => p.S3Key_Thumbnail = "thumb")
                .With(p => p.Storage = storage)
                .With(p => p.StorageId = storage.Id)
                .With(p => p.Name = "withoutTag")
                .Build();
            context.Photos.AddRange(photoWithTag, photoWithoutTag);
            await context.SaveChangesAsync();

            var service = CreateService(dbName);
            var filter = new FilterDto { Tags = new[] { tag.Id } };

            // Act
            var result = await service.GetAllPhotosAsync(filter);

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items.Should().ContainSingle(p => p.Name == "withTag");
            result.Items.Should().NotContain(p => p.Name == "withoutTag");
        }

        [Test]
        public async Task GetAllPhotosAsync_FilterByMultipleTags_ReturnsPhotosWithAllTags()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            services.AddDbContext<PhotoBankDbContext>(o => o.UseInMemoryDatabase(dbName));
            var provider = services.BuildServiceProvider();
            var context = provider.GetRequiredService<PhotoBankDbContext>();

            var storage = Builder<Storage>.CreateNew().With(s => s.Name = "s1").Build();
            context.Storages.Add(storage);
            var tag1 = Builder<Tag>.CreateNew().With(p => p.Id = 1).With(t => t.Name = "Tag1").Build();
            var tag2 = Builder<Tag>.CreateNew().With(p => p.Id = 2).With(t => t.Name = "Tag2").Build();
            context.Tags.AddRange(tag1, tag2);
            await context.SaveChangesAsync();

            var photoAllTags = Builder<Photo>.CreateNew()
                .With(p => p.Id = 1)
                .With(p => p.Location = new Point(0, 0))
                .With(p => p.S3Key_Thumbnail = "thumb")
                .With(p => p.Storage = storage)
                .With(p => p.StorageId = storage.Id)
                .With(p => p.Name = "all")
                .Build();
            photoAllTags.PhotoTags = new List<PhotoTag>
            {
                new PhotoTag { Photo = photoAllTags, Tag = tag1 },
                new PhotoTag { Photo = photoAllTags, Tag = tag2 }
            };

            var photoOnlyTag1 = Builder<Photo>.CreateNew()
                .With(p => p.Id = 2)
                .With(p => p.Location = new Point(0, 0))
                .With(p => p.S3Key_Thumbnail = "thumb")
                .With(p => p.Storage = storage)
                .With(p => p.StorageId = storage.Id)
                .With(p => p.Name = "t1")
                .Build();
            photoOnlyTag1.PhotoTags = new List<PhotoTag>
            {
                new PhotoTag { Photo = photoOnlyTag1, Tag = tag1 }
            };

            var photoOnlyTag2 = Builder<Photo>.CreateNew()
                .With(p => p.Id = 3)
                .With(p => p.Location = new Point(0, 0))
                .With(p => p.S3Key_Thumbnail = "thumb")
                .With(p => p.Storage = storage)
                .With(p => p.StorageId = storage.Id)
                .With(p => p.Name = "t2")
                .Build();
            photoOnlyTag2.PhotoTags = new List<PhotoTag>
            {
                new PhotoTag { Photo = photoOnlyTag2, Tag = tag2 }
            };

            context.Photos.AddRange(photoAllTags, photoOnlyTag1, photoOnlyTag2);
            await context.SaveChangesAsync();

            var service = CreateService(dbName);
            var filter = new FilterDto { Tags = new[] { tag1.Id, tag2.Id, tag1.Id } };

            // Act
            var result = await service.GetAllPhotosAsync(filter);

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items.Should().ContainSingle(p => p.Name == "all");
            result.Items.Should().NotContain(p => p.Name == "t1");
            result.Items.Should().NotContain(p => p.Name == "t2");
        }

        [Test]
        public async Task GetAllPhotosAsync_FilterByMultiplePersons_ReturnsPhotosWithAllPersons()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            services.AddDbContext<PhotoBankDbContext>(o => o.UseInMemoryDatabase(dbName));
            var provider = services.BuildServiceProvider();
            var context = provider.GetRequiredService<PhotoBankDbContext>();

            var storage = Builder<Storage>.CreateNew().With(s => s.Name = "s1").Build();
            context.Storages.Add(storage);
            var person1 = Builder<Person>.CreateNew().With(p=> p.Id = 1).With(p => p.Name = "P1").Build();
            var person2 = Builder<Person>.CreateNew().With(p => p.Id = 2).With(p => p.Name = "P2").Build();
            context.Persons.AddRange(person1, person2);
            await context.SaveChangesAsync();

            var photoBoth = Builder<Photo>.CreateNew()
                .With(p => p.Id = 1)
                .With(p => p.Location = new Point(0, 0))
                .With(p => p.S3Key_Thumbnail = "thumb")
                .With(p => p.Storage = storage)
                .With(p => p.StorageId = storage.Id)
                .With(p => p.Name = "both")
                .Build();
            photoBoth.Faces = new List<Face>
            {
                CreateFace(photoBoth, person1),
                CreateFace(photoBoth, person2)
            };

            var photoOnlyP1 = Builder<Photo>.CreateNew()
                .With(p => p.Id = 2)
                .With(p => p.Location = new Point(0, 0))
                .With(p => p.S3Key_Thumbnail = "thumb")
                .With(p => p.Storage = storage)
                .With(p => p.StorageId = storage.Id)
                .With(p => p.Name = "p1")
                .Build();
            photoOnlyP1.Faces = new List<Face>
            {
                CreateFace(photoOnlyP1, person1)
            };

            var photoOnlyP2 = Builder<Photo>.CreateNew()
                .With(p => p.Id = 3)
                .With(p => p.Location = new Point(0, 0))
                .With(p => p.S3Key_Thumbnail = "thumb")
                .With(p => p.Storage = storage)
                .With(p => p.StorageId = storage.Id)
                .With(p => p.Name = "p2")
                .Build();
            photoOnlyP2.Faces = new List<Face>
            {
                CreateFace(photoOnlyP2, person2)
            };

            context.Photos.AddRange(photoBoth, photoOnlyP1, photoOnlyP2);
            await context.SaveChangesAsync();

            var service = CreateService(dbName);
            var filter = new FilterDto { Persons = new[] { person1.Id, person2.Id, person1.Id } };

            // Act
            var result = await service.GetAllPhotosAsync(filter);

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items.Should().ContainSingle(p => p.Name == "both");
            result.Items.Should().NotContain(p => p.Name == "p1");
            result.Items.Should().NotContain(p => p.Name == "p2");
        }
    }
}
