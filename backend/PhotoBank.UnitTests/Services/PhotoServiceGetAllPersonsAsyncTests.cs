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
    public class PhotoServiceGetAllPersonsAsyncTests
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
        public async Task GetAllPersonsAsync_UserWithoutProfile_ReturnsNoPersons()
        {
            var dbName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            services.AddDbContext<PhotoBankDbContext>(o => o.UseInMemoryDatabase(dbName));
            var provider = services.BuildServiceProvider();
            var context = provider.GetRequiredService<PhotoBankDbContext>();

            var group = new PersonGroup { Name = "g1" };
            var person = new Person { Name = "p1", PersonGroups = new List<PersonGroup> { group } };
            context.PersonGroups.Add(group);
            context.Persons.Add(person);
            await context.SaveChangesAsync();

            var currentUser = new TestCurrentUser { IsAdmin = false };
            var service = CreateService(dbName, currentUser);

            var result = await service.GetAllPersonsAsync();
            result.Should().BeEmpty();
        }

        [Test]
        public async Task GetAllPersonsAsync_WithProfile_ReturnsOnlyAllowed()
        {
            var dbName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            services.AddDbContext<PhotoBankDbContext>(o => o.UseInMemoryDatabase(dbName));
            var provider = services.BuildServiceProvider();
            var context = provider.GetRequiredService<PhotoBankDbContext>();

            var group1 = new PersonGroup { Name = "g1" };
            var group2 = new PersonGroup { Name = "g2" };
            context.PersonGroups.AddRange(group1, group2);

            var person1 = new Person { Name = "p1", PersonGroups = new List<PersonGroup> { group1 } };
            var person2 = new Person { Name = "p2", PersonGroups = new List<PersonGroup> { group2 } };
            context.Persons.AddRange(person1, person2);
            await context.SaveChangesAsync();

            var currentUser = new TestCurrentUser
            {
                IsAdmin = false,
                AllowedPersonGroupIds = new HashSet<int> { group1.Id }
            };

            var service = CreateService(dbName, currentUser);
            var result = await service.GetAllPersonsAsync();

            result.Should().ContainSingle(p => p.Name == "p1");
        }
    }
}
