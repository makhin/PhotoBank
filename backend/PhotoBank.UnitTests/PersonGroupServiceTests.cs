using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using AutoMapper;
using NUnit.Framework;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services;
using PhotoBank.Services.Api;
using PhotoBank.AccessControl;
using Moq;
using Minio;

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
        _provider = services.BuildServiceProvider();

        var db = _provider.GetRequiredService<PhotoBankDbContext>();
        db.Persons.Add(new Person { Id = 1, Name = "John" });
        db.PersonGroups.Add(new PersonGroup { Id = 1, Name = "Family" });
        db.SaveChanges();

        _service = new PhotoService(
            db,
            _provider.GetRequiredService<IRepository<Photo>>(),
            _provider.GetRequiredService<IRepository<Person>>(),
            _provider.GetRequiredService<IRepository<Face>>(),
            _provider.GetRequiredService<IRepository<Storage>>(),
            _provider.GetRequiredService<IRepository<Tag>>(),
            _provider.GetRequiredService<IRepository<PersonGroup>>(),
            _provider.GetRequiredService<IRepository<PersonFace>>(),
            _provider.GetRequiredService<IMapper>(),
            _provider.GetRequiredService<IMemoryCache>(),
            _provider.GetRequiredService<ICurrentUser>(),
            new Mock<IS3ResourceService>().Object,
            new Mock<IMinioClient>().Object
        );
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
}

