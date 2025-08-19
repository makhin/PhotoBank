extern alias ServicesLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FizzWare.NBuilder;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services;
using PhotoBank.Services.Api;
using PhotoBank.ViewModel.Dto;
using AccessCurrentUser = ServicesLib.PhotoBank.AccessControl.ICurrentUser;

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

        private PhotoService CreateService(string dbName, AccessCurrentUser? user = null)
        {
            var services = new ServiceCollection();
            services.AddDbContext<PhotoBankDbContext>(o => o.UseInMemoryDatabase(dbName));
            var provider = services.BuildServiceProvider();
            var context = provider.GetRequiredService<PhotoBankDbContext>();
            user ??= new TestCurrentUser { IsAdmin = true };
            return new PhotoService(
                context,
                new Repository<Photo>(provider),
                new Repository<Person>(provider),
                new Repository<Face>(provider),
                new Repository<Storage>(provider),
                new Repository<Tag>(provider),
                _mapper,
                new MemoryCache(new MemoryCacheOptions()),
                user);
        }

        private sealed class TestCurrentUser : AccessCurrentUser
        {
            public bool IsAdmin { get; init; }
            public IReadOnlySet<int> AllowedStorageIds { get; init; } = new HashSet<int>();
            public IReadOnlySet<int> AllowedPersonGroupIds { get; init; } = new HashSet<int>();
            public IReadOnlyList<(DateOnly From, DateOnly To)> AllowedDateRanges { get; init; } = new List<(DateOnly, DateOnly)>();
            public bool CanSeeNsfw { get; init; } = true;
        }

        [Test]
        public async Task GetPhotoAsync_RespectsAccessControl()
        {
            var dbName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            services.AddDbContext<PhotoBankDbContext>(o => o.UseInMemoryDatabase(dbName));
            var provider = services.BuildServiceProvider();
            var context = provider.GetRequiredService<PhotoBankDbContext>();

            var storage1 = new Storage { Name = "s1" };
            var storage2 = new Storage { Name = "s2" };
            context.Storages.AddRange(storage1, storage2);

            var group1 = new PersonGroup { Name = "g1" };
            var group2 = new PersonGroup { Name = "g2" };
            context.PersonGroups.AddRange(group1, group2);

            var person1 = new Person { Name = "p1", PersonGroups = new List<PersonGroup> { group1 } };
            var person2 = new Person { Name = "p2", PersonGroups = new List<PersonGroup> { group2 } };
            context.Persons.AddRange(person1, person2);
            await context.SaveChangesAsync();

            var allowedPhoto = new Photo { Storage = storage1, StorageId = storage1.Id, Name = "allowed", TakenDate = new DateTime(2020,1,1) };
            allowedPhoto.Faces = new List<Face> { new Face { Photo = allowedPhoto, Person = person1, PersonId = person1.Id } };

            var foreignPhoto = new Photo { Storage = storage1, StorageId = storage1.Id, Name = "foreign", TakenDate = new DateTime(2020,1,1) };
            foreignPhoto.Faces = new List<Face> { new Face { Photo = foreignPhoto, Person = person2, PersonId = person2.Id } };

            var noFacePhoto = new Photo { Storage = storage1, StorageId = storage1.Id, Name = "noface", TakenDate = new DateTime(2020,1,1) };

            var nsfwPhoto = new Photo { Storage = storage1, StorageId = storage1.Id, Name = "nsfw", TakenDate = new DateTime(2020,1,1), IsAdultContent = true };

            var otherStoragePhoto = new Photo { Storage = storage2, StorageId = storage2.Id, Name = "other", TakenDate = new DateTime(2020,1,1) };

            context.Photos.AddRange(allowedPhoto, foreignPhoto, noFacePhoto, nsfwPhoto, otherStoragePhoto);
            await context.SaveChangesAsync();

            var currentUser = new TestCurrentUser
            {
                IsAdmin = false,
                AllowedStorageIds = new HashSet<int> { storage1.Id },
                AllowedPersonGroupIds = new HashSet<int> { group1.Id },
                CanSeeNsfw = false
            };

            var service = CreateService(dbName, currentUser);

            (await service.GetPhotoAsync(allowedPhoto.Id))!.Name.Should().Be("allowed");
            (await service.GetPhotoAsync(foreignPhoto.Id)).Should().BeNull();
            (await service.GetPhotoAsync(noFacePhoto.Id))!.Name.Should().Be("noface");
            (await service.GetPhotoAsync(nsfwPhoto.Id)).Should().BeNull();
            (await service.GetPhotoAsync(otherStoragePhoto.Id)).Should().BeNull();
        }
    }
}
