extern alias ServicesLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
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
using AccessCurrentUser = ServicesLib.PhotoBank.AccessControl.ICurrentUser;

namespace PhotoBank.UnitTests.Services
{
    [TestFixture]
    public class PhotoServiceGetAllStoragesAsyncTests
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
        public async Task GetAllStoragesAsync_UserWithoutProfile_ReturnsNoStorages()
        {
            var dbName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            services.AddDbContext<PhotoBankDbContext>(o => o.UseInMemoryDatabase(dbName));
            var provider = services.BuildServiceProvider();
            var context = provider.GetRequiredService<PhotoBankDbContext>();

            context.Storages.Add(new Storage { Name = "s1" });
            await context.SaveChangesAsync();

            var currentUser = new TestCurrentUser { IsAdmin = false };
            var service = CreateService(dbName, currentUser);

            var result = await service.GetAllStoragesAsync();
            result.Should().BeEmpty();
        }

        [Test]
        public async Task GetAllStoragesAsync_WithProfile_ReturnsOnlyAllowed()
        {
            var dbName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            services.AddDbContext<PhotoBankDbContext>(o => o.UseInMemoryDatabase(dbName));
            var provider = services.BuildServiceProvider();
            var context = provider.GetRequiredService<PhotoBankDbContext>();

            var storage1 = new Storage { Name = "s1" };
            var storage2 = new Storage { Name = "s2" };
            context.Storages.AddRange(storage1, storage2);
            await context.SaveChangesAsync();

            var currentUser = new TestCurrentUser
            {
                IsAdmin = false,
                AllowedStorageIds = new HashSet<int> { storage1.Id }
            };

            var service = CreateService(dbName, currentUser);
            var result = await service.GetAllStoragesAsync();

            result.Should().ContainSingle(s => s.Name == "s1");
        }
    }
}
