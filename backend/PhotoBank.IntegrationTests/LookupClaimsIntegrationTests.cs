using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PhotoBank.DbContext.DbContext;
using PhotoBank.Services;
using PhotoBank.Services.Api;

namespace PhotoBank.IntegrationTests;

[TestFixture]
public class LookupClaimsIntegrationTests
{
    private IConfiguration _config = null!;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        _config = new ConfigurationBuilder()
            .SetBasePath(TestContext.CurrentContext.TestDirectory)
            .AddJsonFile("appsettings.json")
            .Build();
    }

    private ServiceProvider BuildProvider(IEnumerable<Claim>? userClaims = null, IEnumerable<Claim>? roleClaims = null)
    {
        var services = new ServiceCollection();
        var connectionString = _config.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string not found");
        services.AddDbContext<PhotoBankDbContext>(options =>
            options.UseSqlServer(connectionString,
                builder =>
                {
                    builder.MigrationsAssembly(typeof(PhotoBankDbContext).Assembly.GetName().Name);
                    builder.UseNetTopologySuite();
                    builder.CommandTimeout(120);
                }));
        RegisterServicesForApi.Configure(services);
        services.AddLogging();
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });

        var identities = new List<ClaimsIdentity>
        {
            new ClaimsIdentity(userClaims ?? Enumerable.Empty<Claim>(), "User")
        };

        if (roleClaims != null)
        {
            var roleIdentity = new ClaimsIdentity(roleClaims, "Role");
            roleIdentity.AddClaim(new Claim(ClaimTypes.Role, "TestRole"));
            identities.Add(roleIdentity);
        }

        var principal = new ClaimsPrincipal(identities);
        var httpContext = new DefaultHttpContext { User = principal };
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = httpContext });

        return services.BuildServiceProvider();
    }

    [Test]
    public async Task GetAllPersonsAsync_UserClaimRestrictsResults_ReturnsEmpty()
    {
        using var provider = BuildProvider(userClaims: new[] { new Claim("AllowPersonGroup", "-1") });
        var service = provider.GetRequiredService<IPhotoService>();
        var persons = await service.GetAllPersonsAsync();
        persons.Should().BeEmpty();
    }

    [Test]
    public async Task GetAllPersonsAsync_NoUserClaimNoRestrictsResults_ReturnsAll()
    {
        using var provider = BuildProvider(userClaims: Array.Empty<Claim>());
        var service = provider.GetRequiredService<IPhotoService>();
        var persons = await service.GetAllPersonsAsync();
        persons.Count().Should().BeGreaterThan(0);
    }

    [Test]
    public async Task GetAllPersonsAsync_UserClaimRestrictsResults_ReturnsNotEmpty()
    {
        using var provider = BuildProvider(userClaims: new[] { new Claim("AllowPersonGroup", "1") });
        var service = provider.GetRequiredService<IPhotoService>();
        var persons = await service.GetAllPersonsAsync();
        persons.Should().NotBeEmpty();
    }

    [Test]
    public async Task GetAllPersonsAsync_RoleClaimRestrictsResults_ReturnsEmpty()
    {
        using var provider = BuildProvider(roleClaims: new[] { new Claim("AllowPersonGroup", "-1") });
        var service = provider.GetRequiredService<IPhotoService>();
        var persons = await service.GetAllPersonsAsync();
        persons.Should().BeEmpty();
    }

    [Test]
    public async Task GetAllStoragesAsync_UserClaimRestrictsResults_ReturnsEmpty()
    {
        using var provider = BuildProvider(userClaims: new[] { new Claim("AllowStorage", "-1") });
        var service = provider.GetRequiredService<IPhotoService>();
        var storages = await service.GetAllStoragesAsync();
        storages.Should().BeEmpty();
    }

    [Test]
    public async Task GetAllStoragesAsync_RoleClaimRestrictsResults_ReturnsEmpty()
    {
        using var provider = BuildProvider(roleClaims: new[] { new Claim("AllowStorage", "-1") });
        var service = provider.GetRequiredService<IPhotoService>();
        var storages = await service.GetAllStoragesAsync();
        storages.Should().BeEmpty();
    }

    [Test]
    public async Task GetAllTagsAsync_UserClaimDoesNotAffectResults()
    {
        using var baseProvider = BuildProvider();
        var baseService = baseProvider.GetRequiredService<IPhotoService>();
        var expectedCount = (await baseService.GetAllTagsAsync()).Count();

        using var provider = BuildProvider(userClaims: new[] { new Claim("AllowStorage", "-1") });
        var service = provider.GetRequiredService<IPhotoService>();
        var tags = await service.GetAllTagsAsync();
        tags.Should().HaveCount(expectedCount);
    }

    [Test]
    public async Task GetAllTagsAsync_RoleClaimDoesNotAffectResults()
    {
        using var baseProvider = BuildProvider();
        var baseService = baseProvider.GetRequiredService<IPhotoService>();
        var expectedCount = (await baseService.GetAllTagsAsync()).Count();

        using var provider = BuildProvider(roleClaims: new[] { new Claim("AllowStorage", "-1") });
        var service = provider.GetRequiredService<IPhotoService>();
        var tags = await service.GetAllTagsAsync();
        tags.Should().HaveCount(expectedCount);
    }
}