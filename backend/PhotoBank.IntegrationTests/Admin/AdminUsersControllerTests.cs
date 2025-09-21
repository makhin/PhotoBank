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
using Microsoft.AspNetCore.Identity;
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
using PhotoBank.Api;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.ViewModel.Dto;
using Respawn;

namespace PhotoBank.IntegrationTests.Admin;

[TestFixture]
public class AdminUsersControllerTests
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
    public async Task List_WithLimitOffsetAndSort_ReturnsPagedSortedUsers()
    {
        await SeedUsersAsync(
            new ApplicationUser { UserName = "alice@example.com", Email = "alice@example.com", PhoneNumber = "100", TelegramUserId = 1 },
            new ApplicationUser { UserName = "bob@example.com", Email = "bob@example.com", PhoneNumber = "200", TelegramUserId = 2 },
            new ApplicationUser { UserName = "charlie@example.com", Email = "charlie@example.com", PhoneNumber = "300", TelegramUserId = 3 });

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/users?limit=2&offset=1&sort=-email");
        AddAdminHeaders(request);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();
        var users = JsonSerializer.Deserialize<List<UserDto>>(payload, _jsonOptions);

        users.Should().NotBeNull();
        users!.Should().HaveCount(2);
        users.Select(u => u.Email).Should().ContainInOrder("bob@example.com", "alice@example.com");
    }

    [Test]
    public async Task List_WithFilters_ReturnsOnlyMatchingUsers()
    {
        await SeedUsersAsync(
            new ApplicationUser { UserName = "john@example.com", Email = "john@example.com", PhoneNumber = "111", TelegramUserId = 11 },
            new ApplicationUser { UserName = "jane@example.com", Email = "jane@example.com", PhoneNumber = "222", TelegramUserId = null },
            new ApplicationUser { UserName = "josh@example.com", Email = "josh@example.com", PhoneNumber = "333", TelegramUserId = 33 });

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/users?search=j&hasTelegram=false&sort=phone");
        AddAdminHeaders(request);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();
        var users = JsonSerializer.Deserialize<List<UserDto>>(payload, _jsonOptions);

        users.Should().NotBeNull();
        users!.Should().HaveCount(1);
        users[0].Email.Should().Be("jane@example.com");
        users[0].PhoneNumber.Should().Be("222");
    }

    [Test]
    public async Task List_WithHasTelegramTrueAndTelegramSort_ReturnsOnlyTelegramUsers()
    {
        await SeedUsersAsync(
            new ApplicationUser { UserName = "adam@example.com", Email = "adam@example.com", TelegramUserId = 44 },
            new ApplicationUser { UserName = "bella@example.com", Email = "bella@example.com", TelegramUserId = null },
            new ApplicationUser { UserName = "carol@example.com", Email = "carol@example.com", TelegramUserId = 11 });

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/users?hasTelegram=true&sort=-telegram");
        AddAdminHeaders(request);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();
        var users = JsonSerializer.Deserialize<List<UserDto>>(payload, _jsonOptions);

        users.Should().NotBeNull();
        users!.Select(u => u.TelegramUserId).Should().Equal(44, 11);
    }

    [Test]
    public async Task List_WhenNoUsers_ReturnsEmptyArray()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/users");
        AddAdminHeaders(request);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();
        var users = JsonSerializer.Deserialize<List<UserDto>>(payload, _jsonOptions);

        users.Should().NotBeNull();
        users.Should().BeEmpty();
    }

    [Test]
    public async Task Create_WhenDuplicateEmail_ReturnsConflict()
    {
        var existing = new ApplicationUser
        {
            UserName = "duplicate@example.com",
            Email = "duplicate@example.com"
        };
        await SeedUsersAsync(existing);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/users");
        AddAdminHeaders(request);
        var dto = new CreateUserDto
        {
            Email = "duplicate@example.com",
            Password = "P@ssw0rd!",
            PhoneNumber = "999"
        };
        request.Content = JsonContent.Create(dto);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task List_WithInvalidLimit_ReturnsValidationProblem()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/users?limit=0");
        AddAdminHeaders(request);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static void AddAdminHeaders(HttpRequestMessage request)
    {
        request.Headers.Add(UserHeader, "admin");
        request.Headers.Add(RolesHeader, AdminRole);
    }

    private async Task SeedUsersAsync(params ApplicationUser[] users)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var user in users)
        {
            var result = await userManager.CreateAsync(user, "P@ssw0rd!");
            result.Succeeded.Should().BeTrue(result.Errors.FirstOrDefault()?.Description);
        }
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
