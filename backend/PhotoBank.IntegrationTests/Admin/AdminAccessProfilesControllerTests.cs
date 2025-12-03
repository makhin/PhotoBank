using DotNet.Testcontainers.Builders;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Npgsql;
using NUnit.Framework;
using PhotoBank.AccessControl;
using PhotoBank.DbContext.DbContext;
using PhotoBank.IntegrationTests.Infra;
using Respawn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;

namespace PhotoBank.IntegrationTests.Admin;

[TestFixture]
[Category("Integration")]
public class AdminAccessProfilesControllerTests
{
    private const string AdminRole = "Admin";

    private PostgreSqlContainer _dbContainer = null!;
    private Respawner _respawner = null!;
    private string _connectionString = string.Empty;
    private ApiWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private JsonSerializerOptions _jsonOptions = null!;
    private Mock<IEffectiveAccessProvider> _effMock = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        // Configure Npgsql to treat DateTime with Kind=Unspecified as UTC
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        try
        {
            _dbContainer = new PostgreSqlBuilder()
                .WithImage("kristofdetroch/postgres-postgis-pgvector:latest")
                .WithPassword("postgres")
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

        var photoDbOptionsBuilder = new DbContextOptionsBuilder<PhotoBankDbContext>();

        photoDbOptionsBuilder.ConfigureWarnings(w =>
            w.Log(RelationalEventId.PendingModelChangesWarning));

        photoDbOptionsBuilder.UseNpgsql(_connectionString, builder =>
        {
            builder.MigrationsAssembly(typeof(PhotoBankDbContext).Assembly.GetName().Name);
            builder.MigrationsHistoryTable("__EFMigrationsHistory_Photo");
            builder.UseVector();
            builder.UseNetTopologySuite();
        });

        var photoDbOptions = photoDbOptionsBuilder.Options;

        await using (var photoDb = new PhotoBankDbContext(photoDbOptions))
            await photoDb.Database.MigrateAsync();

        var asm1 = typeof(PhotoBankDbContext).Assembly.GetName().Name;
        Console.WriteLine($"[DBG] PhotoBankDbContext runtime MigrationsAssembly = {asm1}");

        var accessDbOptionsBuilder = new DbContextOptionsBuilder<AccessControlDbContext>();
        accessDbOptionsBuilder.ConfigureWarnings(w =>
            w.Log(RelationalEventId.PendingModelChangesWarning));

        accessDbOptionsBuilder.UseNpgsql(_connectionString, builder =>
        {
            builder.MigrationsAssembly(typeof(AccessControlDbContext).Assembly.GetName().Name);
            builder.MigrationsHistoryTable("__EFMigrationsHistory_Access");
            builder.UseVector();
            builder.UseNetTopologySuite();
        });

        var acDbOptions = accessDbOptionsBuilder.Options;

        await using (var accessDb = new AccessControlDbContext(acDbOptions))
            await accessDb.Database.MigrateAsync();
        var asm = typeof(AccessControlDbContext).Assembly.GetName().Name;
        Console.WriteLine($"[DBG] AccessControlDbContext runtime MigrationsAssembly = {asm}");

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            TablesToIgnore = new[]
            {
                new Respawn.Graph.Table("__EFMigrationsHistory_Photo"),
                new Respawn.Graph.Table("__EFMigrationsHistory_Access")
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

        await using (var conn = new NpgsqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            await _respawner.ResetAsync(conn);
        }

        var configuration = TestConfiguration.Build(_connectionString);

        _factory = new ApiWebApplicationFactory(
            configuration: configuration,
            configureServices: services =>
            {
                services.AddTestAuthentication();
                services.RemoveAll<IEffectiveAccessProvider>();
                _effMock = new Mock<IEffectiveAccessProvider>();

                // Setup mock to return a valid EffectiveAccess for admin users
                _effMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new EffectiveAccess(
                        new HashSet<int>(),
                        new HashSet<int>(),
                        new List<(DateOnly, DateOnly)>(),
                        false, // CanSeeNsfw
                        true   // IsAdmin
                    ));

                services.AddSingleton(_effMock.Object);
            });
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
        _client?.Dispose();
        _factory?.Dispose();
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
    public async Task AssignUser_WhenNewAssignment_InvalidatesEffectiveAccess()
    {
        var seeded = await SeedProfileAsync(new AccessProfile
        {
            Name = "Theta",
            Description = "Assign",
            Flags_CanSeeNsfw = false
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/access-profiles/{seeded.Id}/assign-user/user-assign");
        AddAdminHeaders(request);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        _effMock.Verify(x => x.Invalidate("user-assign"), Times.Once);
    }

    [Test]
    public async Task UnassignUser_WhenAssignmentExists_InvalidatesEffectiveAccess()
    {
        var seeded = await SeedProfileAsync(new AccessProfile
        {
            Name = "Iota",
            Description = "Unassign",
            Flags_CanSeeNsfw = false
        });

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AccessControlDbContext>();
            db.UserAccessProfiles.Add(new UserAccessProfile { ProfileId = seeded.Id, UserId = "user-unassign" });
            await db.SaveChangesAsync();
        }

        _effMock.Invocations.Clear();

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/access-profiles/{seeded.Id}/assign-user/user-unassign");
        AddAdminHeaders(request);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        _effMock.Verify(x => x.Invalidate("user-unassign"), Times.Once);
    }

    [Test]
    public async Task Update_WhenProfileUpdated_InvalidatesAssignedUsers()
    {
        var seeded = await SeedProfileAsync(new AccessProfile
        {
            Name = "Kappa",
            Description = "Invalidate",
            Flags_CanSeeNsfw = false
        });

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AccessControlDbContext>();
            db.UserAccessProfiles.Add(new UserAccessProfile { ProfileId = seeded.Id, UserId = "user-update" });
            await db.SaveChangesAsync();
        }

        _effMock.Invocations.Clear();

        var payload = new AccessProfile
        {
            Id = seeded.Id,
            Name = "Kappa Updated",
            Description = "Invalidate",
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
        _effMock.Verify(x => x.Invalidate("user-update"), Times.Once);
    }

    [Test]
    public async Task Delete_WhenProfileDeleted_InvalidatesAssignedUsers()
    {
        var seeded = await SeedProfileAsync(new AccessProfile
        {
            Name = "Lambda",
            Description = "Delete Invalidate",
            Flags_CanSeeNsfw = false
        });

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AccessControlDbContext>();
            db.UserAccessProfiles.Add(new UserAccessProfile { ProfileId = seeded.Id, UserId = "user-delete" });
            await db.SaveChangesAsync();
        }

        _effMock.Invocations.Clear();

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/access-profiles/{seeded.Id}");
        AddAdminHeaders(request);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        _effMock.Verify(x => x.Invalidate("user-delete"), Times.Once);
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
        request.Headers.Add(TestAuthenticationDefaults.UserHeader, "user");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static void AddAdminHeaders(HttpRequestMessage request)
    {
        request.Headers.Add(TestAuthenticationDefaults.UserHeader, "admin");
        request.Headers.Add(TestAuthenticationDefaults.RolesHeader, AdminRole);
    }

    private async Task<AccessProfile> SeedProfileAsync(AccessProfile profile)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AccessControlDbContext>();
        db.AccessProfiles.Add(profile);
        await db.SaveChangesAsync();
        return profile;
    }

}
