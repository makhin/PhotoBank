using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FizzWare.NBuilder;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services;
using PhotoBank.Services.Api;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.UnitTests.Services
{
    [TestFixture]
    public class PhotoServiceGetAllPhotosAsyncTests
    {
        private IMapper _mapper = null!;

        [SetUp]
        public void Setup()
        {
            var mappingProfile = new MappingProfile();
            var config = new MapperConfiguration(c => c.AddProfile(mappingProfile));
            _mapper = new Mapper(config);
        }

        private PhotoService CreateService(string dbName)
        {
            var services = new ServiceCollection();
            services.AddDbContext<PhotoBankDbContext>(o => o.UseInMemoryDatabase(dbName));
            var provider = services.BuildServiceProvider();
            return new PhotoService(
                new Repository<Photo>(provider),
                new Repository<Person>(provider),
                new Repository<Face>(provider),
                new Repository<Storage>(provider),
                new Repository<Tag>(provider),
                _mapper);
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
            result.Count.Should().Be(2);
            result.Photos.Should().HaveCount(2);
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
                .With(p => p.Storage = storage)
                .With(p => p.StorageId = storage.Id)
                .With(p => p.Name = "bw")
                .Build();
            var colorPhoto = Builder<Photo>.CreateNew()
                .With(p => p.Id = 2)
                .With(p => p.IsBW = false)
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
            result.Count.Should().Be(1);
            result.Photos.Should().ContainSingle(p => p.Name == "bw");
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
            result.Count.Should().Be(1);
            result.Photos.Should().ContainSingle(p => p.Name == "withTag");
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
            var filter = new FilterDto { Tags = new[] { tag1.Id, tag2.Id } };

            // Act
            var result = await service.GetAllPhotosAsync(filter);

            // Assert
            result.Count.Should().Be(1);
            result.Photos.Should().ContainSingle(p => p.Name == "all");
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
                .With(p => p.Storage = storage)
                .With(p => p.StorageId = storage.Id)
                .With(p => p.Name = "both")
                .Build();
            photoBoth.Faces = new List<Face>
            {
                new Face { Photo = photoBoth, Person = person1, PersonId = person1.Id },
                new Face { Photo = photoBoth, Person = person2, PersonId = person2.Id }
            };

            var photoOnlyP1 = Builder<Photo>.CreateNew()
                .With(p => p.Id = 2)
                .With(p => p.Storage = storage)
                .With(p => p.StorageId = storage.Id)
                .With(p => p.Name = "p1")
                .Build();
            photoOnlyP1.Faces = new List<Face>
            {
                new Face { Photo = photoOnlyP1, Person = person1, PersonId = person1.Id }
            };

            var photoOnlyP2 = Builder<Photo>.CreateNew()
                .With(p => p.Id = 3)
                .With(p => p.Storage = storage)
                .With(p => p.StorageId = storage.Id)
                .With(p => p.Name = "p2")
                .Build();
            photoOnlyP2.Faces = new List<Face>
            {
                new Face { Photo = photoOnlyP2, Person = person2, PersonId = person2.Id }
            };

            context.Photos.AddRange(photoBoth, photoOnlyP1, photoOnlyP2);
            await context.SaveChangesAsync();

            var service = CreateService(dbName);
            var filter = new FilterDto { Persons = new[] { person1.Id, person2.Id } };

            // Act
            var result = await service.GetAllPhotosAsync(filter);

            // Assert
            result.Count.Should().Be(1);
            result.Photos.Should().ContainSingle(p => p.Name == "both");
        }
    }
}