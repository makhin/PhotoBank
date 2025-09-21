using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using Testcontainers.MsSql;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Moq;
using NUnit.Framework;
using PhotoBank.AccessControl;
using PhotoBank.Api;
using PhotoBank.DbContext.DbContext;
using Respawn;

namespace PhotoBank.IntegrationTests.Admin;

[TestFixture]
public class AdminAccessProfilesControllerTests
{
    private const string AdminRole = "Admin";
    private const string UserHeader = "X-Test-User";
    private const string RolesHeader = "X-Test-Roles";

    private MsSqlContainer _dbContainer = null!;
    private Respawner _respawner = null!;
    private string _connectionString = string.Empty;
    private TestWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private JsonSerializerOptions _jsonOptions = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        try
        {
            _dbContainer = new MsSqlBuilder()
                .WithPassword("yourStrong(!)Password")
                .Build();
            await _dbContainer.StartAsync();
        }
        catch (ArgumentException ex) when (ex.Message.Contains("Docker endpoint"))
        {
            Assert.Ignore("Docker not available: " + ex.Message);
        }
        catch (DockerUnavailableException ex)
        {
            Assert.Ignore("Docker not available: " + ex.Message);
        }

        _connectionString = _dbContainer.GetConnectionString();

        var photoDbOptions = new DbContextOptionsBuilder<PhotoBankDbContext>()
            .UseSqlServer(_connectionString, builder =>
            {
                builder.MigrationsAssembly(typeof(PhotoBankDbContext).Assembly.GetName().Name);
                builder.UseNetTopologySuite();
            })
            .Options;

        await using (var photoDb = new PhotoBankDbContext(photoDbOptions))
        {
            await photoDb.Database.MigrateAsync();
        }

        var accessDbOptions = new DbContextOptionsBuilder<AccessControlDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        await using (var accessDb = new AccessControlDbContext(accessDbOptions))
        {
            await accessDb.Database.MigrateAsync();
        }

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            TablesToIgnore = new[]
            {
                new Respawn.Graph.Table("__EFMigrationsHistory")
            }
        });
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_dbContainer != null)
        {
            await _dbContainer.DisposeAsync();
        }
    }

    [SetUp]
    public async Task Setup()
    {
        if (_respawner == null)
        {
            Assert.Ignore("Database respawner is not available.");
        }

        await using (var conn = new SqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            await _respawner.ResetAsync(conn);
        }

        _factory = new TestWebApplicationFactory(_connectionString);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });

        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task List_WhenProfilesExist_ReturnsAllProfilesWithDetails()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        await SeedProfileAsync(new AccessProfile
        {
            Name = "Alpha",
            Description = "First",
            Flags_CanSeeNsfw = true,
            Storages = new List<AccessProfileStorageAllow>
            {
                new() { StorageId = 5 },
                new() { StorageId = 7 }
            },
            PersonGroups = new List<AccessProfilePersonGroupAllow>
            {
                new() { PersonGroupId = 11 }
            },
            DateRanges = new List<AccessProfileDateRangeAllow>
            {
                new() { FromDate = today, ToDate = today.AddDays(10) }
            }
        });

        await SeedProfileAsync(new AccessProfile
        {
            Name = "Beta",
            Description = "Second",
            Flags_CanSeeNsfw = false
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/access-profiles");
        AddAdminHeaders(request);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();
        var profiles = JsonSerializer.Deserialize<List<AccessProfile>>(payload, _jsonOptions);
        profiles.Should().NotBeNull();
        profiles!.Select(p => p.Name).Should().ContainInOrder("Alpha", "Beta");

        var alpha = profiles.First(p => p.Name == "Alpha");
        alpha.Description.Should().Be("First");
        alpha.Flags_CanSeeNsfw.Should().BeTrue();
        alpha.Storages.Should().HaveCount(2);
        alpha.PersonGroups.Should().HaveCount(1);
        alpha.DateRanges.Should().HaveCount(1);
    }

    [Test]
    public async Task Get_WhenProfileExists_ReturnsProfile()
    {
        var seeded = await SeedProfileAsync(new AccessProfile
        {
            Name = "Gamma",
            Description = "Profile",
            Flags_CanSeeNsfw = true
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/admin/access-profiles/{seeded.Id}");
        AddAdminHeaders(request);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();
        var profile = JsonSerializer.Deserialize<AccessProfile>(payload, _jsonOptions);
        profile.Should().NotBeNull();
        profile!.Id.Should().Be(seeded.Id);
        profile.Name.Should().Be("Gamma");
        profile.Description.Should().Be("Profile");
    }

    [Test]
    public async Task Get_WhenProfileMissing_ReturnsNotFound()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/access-profiles/12345");
        AddAdminHeaders(request);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Create_WhenValid_ReturnsCreatedProfile()
    {
        var newProfile = new AccessProfile
        {
            Name = "Delta",
            Description = "Created",
            Flags_CanSeeNsfw = true,
            Storages = new List<AccessProfileStorageAllow>
            {
                new() { StorageId = 3 }
            },
            PersonGroups = new List<AccessProfilePersonGroupAllow>
            {
                new() { PersonGroupId = 2 }
            },
            DateRanges = new List<AccessProfileDateRangeAllow>
            {
                new()
                {
                    FromDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
                    ToDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(5))
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/access-profiles")
        {
            Content = JsonContent.Create(newProfile, options: new JsonSerializerOptions { PropertyNamingPolicy = null })
        };
        AddAdminHeaders(request);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var payload = await response.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<AccessProfile>(payload, _jsonOptions);
        created.Should().NotBeNull();
        created!.Id.Should().BeGreaterThan(0);
        created.Name.Should().Be("Delta");
        created.Storages.Should().HaveCount(1);
        created.PersonGroups.Should().HaveCount(1);
        created.DateRanges.Should().HaveCount(1);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AccessControlDbContext>();
        var fromDb = await db.AccessProfiles.Include(p => p.Storages).Include(p => p.PersonGroups).Include(p => p.DateRanges)
            .FirstOrDefaultAsync(p => p.Id == created.Id);
        fromDb.Should().NotBeNull();
        fromDb!.Name.Should().Be("Delta");
    }

    [Test]
    public async Task Update_WhenValid_ReturnsNoContentAndPersistsChanges()
    {
        var seeded = await SeedProfileAsync(new AccessProfile
        {
            Name = "Epsilon",
            Description = "Old",
            Flags_CanSeeNsfw = false
        });

        var payload = new AccessProfile
        {
            Id = seeded.Id,
            Name = "Epsilon Updated",
            Description = "Updated",
            Flags_CanSeeNsfw = true,
            Storages = new List<AccessProfileStorageAllow>(),
            PersonGroups = new List<AccessProfilePersonGroupAllow>(),
            DateRanges = new List<AccessProfileDateRangeAllow>()
        };

        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/admin/access-profiles/{seeded.Id}")
        {
            Content = JsonContent.Create(payload, options: new JsonSerializerOptions { PropertyNamingPolicy = null })
        };
        AddAdminHeaders(request);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AccessControlDbContext>();
        var updated = await db.AccessProfiles.FindAsync(seeded.Id);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Epsilon Updated");
        updated.Description.Should().Be("Updated");
        updated.Flags_CanSeeNsfw.Should().BeTrue();
    }

    [Test]
    public async Task Update_WhenIdMismatch_ReturnsBadRequest()
    {
        var seeded = await SeedProfileAsync(new AccessProfile
        {
            Name = "Zeta",
            Description = "Mismatch",
            Flags_CanSeeNsfw = false
        });

        var payload = new AccessProfile
        {
            Id = seeded.Id + 1,
            Name = "Zeta",
            Description = "Mismatch",
            Flags_CanSeeNsfw = false,
            Storages = new List<AccessProfileStorageAllow>(),
            PersonGroups = new List<AccessProfilePersonGroupAllow>(),
            DateRanges = new List<AccessProfileDateRangeAllow>()
        };

        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/admin/access-profiles/{seeded.Id}")
        {
            Content = JsonContent.Create(payload, options: new JsonSerializerOptions { PropertyNamingPolicy = null })
        };
        AddAdminHeaders(request);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Delete_WhenProfileExists_ReturnsNoContent()
    {
        var seeded = await SeedProfileAsync(new AccessProfile
        {
            Name = "Eta",
            Description = "Delete",
            Flags_CanSeeNsfw = false
        });

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/access-profiles/{seeded.Id}");
        AddAdminHeaders(request);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AccessControlDbContext>();
        var deleted = await db.AccessProfiles.FindAsync(seeded.Id);
        deleted.Should().BeNull();
    }

    [Test]
    public async Task Delete_WhenProfileMissing_ReturnsNotFound()
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/admin/access-profiles/6789");
        AddAdminHeaders(request);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task List_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/admin/access-profiles");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task List_WhenUserWithoutAdminRole_ReturnsForbidden()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/access-profiles");
        request.Headers.Add(UserHeader, "user");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static void AddAdminHeaders(HttpRequestMessage request)
    {
        request.Headers.Add(UserHeader, "admin");
        request.Headers.Add(RolesHeader, AdminRole);
    }

    private async Task<AccessProfile> SeedProfileAsync(AccessProfile profile)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AccessControlDbContext>();
        db.AccessProfiles.Add(profile);
        await db.SaveChangesAsync();
        return profile;
    }

    private sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;

        public TestWebApplicationFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(Environments.Development);
            builder.ConfigureAppConfiguration((context, configBuilder) =>
            {
                var overrides = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _connectionString,
                    ["Jwt:Issuer"] = "issuer",
                    ["Jwt:Audience"] = "audience",
                    ["Jwt:Key"] = "super-secret"
                };
                configBuilder.AddInMemoryCollection(overrides);
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IMinioClient>();
                services.AddSingleton(Mock.Of<IMinioClient>());

                services.RemoveAll<IEffectiveAccessProvider>();
                services.AddSingleton(Mock.Of<IEffectiveAccessProvider>());

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    options.DefaultScheme = TestAuthHandler.SchemeName;
                }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            });
        }
    }

    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "Test";

        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(UserHeader, out var userValues))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var user = userValues.ToString();
            if (string.IsNullOrWhiteSpace(user))
            {
                return Task.FromResult(AuthenticateResult.Fail("User header missing"));
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user),
                new(ClaimTypes.Name, user)
            };

            if (Request.Headers.TryGetValue(RolesHeader, out var rolesValues))
            {
                var roles = rolesValues.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
