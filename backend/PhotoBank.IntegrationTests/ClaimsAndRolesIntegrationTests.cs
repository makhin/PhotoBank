using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Services;

namespace PhotoBank.IntegrationTests;

[TestFixture]
public class ClaimsAndRolesIntegrationTests
{
    private ServiceProvider _provider = null!;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddHttpContextAccessor();
        var dbName = Guid.NewGuid().ToString();
        services.AddDbContext<PhotoBankDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        RegisterServicesForApi.Configure(services);
        services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
        _provider = services.BuildServiceProvider();

        using var scope = _provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PhotoBankDbContext>();
        context.Persons.AddRange(
            new Person { Id = 1, Name = "Alice" },
            new Person { Id = 2, Name = "Bob" });
        context.Tags.AddRange(
            new Tag { Id = 1, Name = "Nature" },
            new Tag { Id = 2, Name = "City" });
        context.Storages.AddRange(
            new Storage { Id = 1, Name = "Local", Folder = "L" },
            new Storage { Id = 2, Name = "Cloud", Folder = "C" });
        context.SaveChanges();
    }

    [TearDown]
    public void TearDown()
    {
        _provider.Dispose();
    }

    [Test]
    public async Task LoadAllEntities_NoClaims_ReturnsAll()
    {
        using var scope = _provider.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };
        var context = scope.ServiceProvider.GetRequiredService<PhotoBankDbContext>();
        var persons = await context.Persons.ToListAsync();
        var tags = await context.Tags.ToListAsync();
        var storages = await context.Storages.ToListAsync();

        persons.Should().HaveCount(2);
        tags.Should().HaveCount(2);
        storages.Should().HaveCount(2);
    }

    [Test]
    public async Task LoadStorages_WithAllowStorageUserClaim_ReturnsAll()
    {
        using var scope = _provider.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("AllowStorage", "1") }, "Test"))
        };
        var context = scope.ServiceProvider.GetRequiredService<PhotoBankDbContext>();
        var storages = await context.Storages.ToListAsync();
        storages.Should().HaveCount(2);
    }

    [Test]
    public async Task LoadStorages_WithAllowStorageRoleClaim_ReturnsAll()
    {
        using var scope = _provider.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Role, "Manager"),
                new Claim("AllowStorage", "2")
            }, "Test"))
        };
        var context = scope.ServiceProvider.GetRequiredService<PhotoBankDbContext>();
        var storages = await context.Storages.ToListAsync();
        storages.Should().HaveCount(2);
    }
}