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
using PhotoBank.AccessControl;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services;
using PhotoBank.Services.Internal;
using PhotoBank.Services.Search;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.UnitTests;

[TestFixture]
public class SearchReferenceDataServiceTests
{
    [Test]
    public async Task InvalidateStorages_ShouldRefreshStoragesAndPathsWithAclAndOrdering()
    {
        using var fixture = new ReferenceServiceFixture(isAdmin: false, allowedStorages: new[] { 1 });
        var context = fixture.Context;
        var service = fixture.Service;
        var user = fixture.User;

        var storage1 = new Storage { Id = 1, Name = "B Storage", Folder = "root1" };
        context.Storages.Add(storage1);
        context.Photos.AddRange(
            CreatePhoto(1, storage1, "photo-1", "folder/b"),
            CreatePhoto(2, storage1, "photo-2", "folder/a"));
        await context.SaveChangesAsync();

        var storagesInitial = await service.GetStoragesAsync();
        storagesInitial.Select(s => s.Id).Should().Equal(1);
        storagesInitial.Select(s => s.Name).Should().Equal("B Storage");

        var pathsInitial = await service.GetPathsAsync();
        pathsInitial.Select(p => (p.StorageId, p.Path)).Should().Equal((1, "folder/a"), (1, "folder/b"));

        var storage2 = new Storage { Id = 2, Name = "A Storage", Folder = "root2" };
        context.Storages.Add(storage2);
        context.Photos.AddRange(
            CreatePhoto(3, storage1, "photo-3", "folder/c"),
            CreatePhoto(4, storage2, "photo-4", "alpha/path"));
        await context.SaveChangesAsync();
        user.AllowStorage(2);

        var storagesCached = await service.GetStoragesAsync();
        storagesCached.Select(s => s.Id).Should().Equal(1);

        var pathsCached = await service.GetPathsAsync();
        pathsCached.Select(p => p.Path).Should().Equal("folder/a", "folder/b");

        service.InvalidateStorages();

        var storagesUpdated = await service.GetStoragesAsync();
        storagesUpdated.Select(s => (s.Id, s.Name))
            .Should()
            .Equal((2, "A Storage"), (1, "B Storage"));

        var pathsUpdated = await service.GetPathsAsync();
        pathsUpdated.Select(p => (p.StorageId, p.Path))
            .Should()
            .Equal((1, "folder/a"), (1, "folder/b"), (1, "folder/c"), (2, "alpha/path"));
    }

    [Test]
    public async Task GetPersonsAsync_ShouldRespectAclAndCacheUntilInvalidated()
    {
        using var fixture = new ReferenceServiceFixture(
            isAdmin: false,
            allowedStorages: new[] { 1 },
            allowedPersonGroupIds: new[] { 1 });
        var context = fixture.Context;
        var service = fixture.Service;
        var user = fixture.User;

        var groupOne = new PersonGroup { Id = 1, Name = "Group One" };
        var groupTwo = new PersonGroup { Id = 2, Name = "Group Two" };
        context.PersonGroups.AddRange(groupOne, groupTwo);

        context.Persons.AddRange(
            new Person
            {
                Id = 1,
                Name = "Alice",
                ExternalGuid = Guid.NewGuid(),
                Faces = new List<Face>(),
                PersonGroups = new List<PersonGroup> { groupOne }
            },
            new Person
            {
                Id = 2,
                Name = "Charlie",
                ExternalGuid = Guid.NewGuid(),
                Faces = new List<Face>(),
                PersonGroups = new List<PersonGroup> { groupTwo }
            });
        await context.SaveChangesAsync();

        var initial = await service.GetPersonsAsync();
        initial.Select(p => p.Name).Should().Equal("Alice");

        context.Persons.AddRange(
            new Person
            {
                Id = 3,
                Name = "Aaron",
                ExternalGuid = Guid.NewGuid(),
                Faces = new List<Face>(),
                PersonGroups = new List<PersonGroup> { groupOne }
            },
            new Person
            {
                Id = 4,
                Name = "Bob",
                ExternalGuid = Guid.NewGuid(),
                Faces = new List<Face>(),
                PersonGroups = new List<PersonGroup> { groupTwo }
            });
        await context.SaveChangesAsync();

        user.AllowPersonGroup(2);

        var cached = await service.GetPersonsAsync();
        cached.Select(p => p.Name).Should().Equal("Alice");

        service.InvalidatePersons();

        var refreshed = await service.GetPersonsAsync();
        refreshed.Select(p => p.Name).Should().Equal("Aaron", "Alice", "Bob", "Charlie");
    }

    [Test]
    public async Task GetPersonGroupsAsync_ShouldReturnSortedAndRefreshOnInvalidation()
    {
        using var fixture = new ReferenceServiceFixture(isAdmin: true, allowedStorages: Array.Empty<int>());
        var context = fixture.Context;
        var service = fixture.Service;

        context.PersonGroups.AddRange(
            new PersonGroup { Id = 1, Name = "Zeta" },
            new PersonGroup { Id = 2, Name = "Alpha" });
        await context.SaveChangesAsync();

        var groups = await service.GetPersonGroupsAsync();
        groups.Select(g => g.Name).Should().Equal("Alpha", "Zeta");

        context.PersonGroups.Add(new PersonGroup { Id = 3, Name = "Beta" });
        await context.SaveChangesAsync();

        var cached = await service.GetPersonGroupsAsync();
        cached.Select(g => g.Name).Should().Equal("Alpha", "Zeta");

        service.InvalidatePersonGroups();

        var refreshed = await service.GetPersonGroupsAsync();
        refreshed.Select(g => g.Name).Should().Equal("Alpha", "Beta", "Zeta");
    }

    private static Photo CreatePhoto(int id, Storage storage, string name, string relativePath) => new()
    {
        Id = id,
        StorageId = storage.Id,
        Storage = storage,
        Name = name,
        RelativePath = relativePath,
        AccentColor = string.Empty,
        DominantColorBackground = string.Empty,
        DominantColorForeground = string.Empty,
        DominantColors = string.Empty,
        S3Key_Preview = string.Empty,
        S3ETag_Preview = string.Empty,
        Sha256_Preview = string.Empty,
        S3Key_Thumbnail = string.Empty,
        S3ETag_Thumbnail = string.Empty,
        Sha256_Thumbnail = string.Empty,
        ImageHash = string.Empty,
        Captions = new List<Caption>(),
        PhotoTags = new List<PhotoTag>(),
        PhotoCategories = new List<PhotoCategory>(),
        ObjectProperties = new List<ObjectProperty>(),
        Faces = new List<Face>(),
        Files = new List<File>()
    };

    private sealed class ReferenceServiceFixture : IDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly IServiceScope _scope;

        public ReferenceServiceFixture(
            bool isAdmin,
            IEnumerable<int> allowedStorages,
            IEnumerable<int>? allowedPersonGroupIds = null)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<PhotoBankDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
            services.AddMemoryCache();

            _provider = services.BuildServiceProvider();
            _scope = _provider.CreateScope();

            Context = _scope.ServiceProvider.GetRequiredService<PhotoBankDbContext>();
            Cache = _scope.ServiceProvider.GetRequiredService<IMemoryCache>();
            var mapper = _scope.ServiceProvider.GetRequiredService<IMapper>();
            var personRepository = _scope.ServiceProvider.GetRequiredService<IRepository<Person>>();
            var tagRepository = _scope.ServiceProvider.GetRequiredService<IRepository<Tag>>();
            var photoRepository = _scope.ServiceProvider.GetRequiredService<IRepository<Photo>>();
            var storageRepository = _scope.ServiceProvider.GetRequiredService<IRepository<Storage>>();
            var personGroupRepository = _scope.ServiceProvider.GetRequiredService<IRepository<PersonGroup>>();

            User = new TestCurrentUser(
                isAdmin ? "admin" : "user",
                isAdmin,
                allowedStorages,
                allowedPersonGroupIds ?? Array.Empty<int>());
            Service = new SearchReferenceDataService(
                personRepository,
                tagRepository,
                photoRepository,
                storageRepository,
                personGroupRepository,
                User,
                Cache,
                mapper);
        }

        public SearchReferenceDataService Service { get; }
        public PhotoBankDbContext Context { get; }
        public TestCurrentUser User { get; }
        public IMemoryCache Cache { get; }

        public void Dispose()
        {
            _scope.Dispose();
            _provider.Dispose();
        }
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        private readonly HashSet<int> _allowedStorageIds;
        private readonly HashSet<int> _allowedPersonGroupIds;

        public TestCurrentUser(
            string userId,
            bool isAdmin,
            IEnumerable<int> allowedStorages,
            IEnumerable<int> allowedPersonGroupIds)
        {
            UserId = userId;
            IsAdmin = isAdmin;
            _allowedStorageIds = new HashSet<int>(allowedStorages);
            _allowedPersonGroupIds = new HashSet<int>(allowedPersonGroupIds);
        }

        public string UserId { get; }
        public bool IsAdmin { get; }
        public IReadOnlySet<int> AllowedStorageIds => _allowedStorageIds;
        public IReadOnlySet<int> AllowedPersonGroupIds => _allowedPersonGroupIds;
        public IReadOnlyList<(DateOnly From, DateOnly To)> AllowedDateRanges { get; } = new List<(DateOnly, DateOnly)>();
        public bool CanSeeNsfw => true;

        public void AllowStorage(int storageId) => _allowedStorageIds.Add(storageId);

        public void AllowPersonGroup(int groupId) => _allowedPersonGroupIds.Add(groupId);
    }
}
