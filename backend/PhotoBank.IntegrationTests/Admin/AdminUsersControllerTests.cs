using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using Testcontainers.MsSql;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.ViewModel.Dto;
using Respawn;
using PhotoBank.IntegrationTests.Infra;

namespace PhotoBank.IntegrationTests.Admin;

[TestFixture]
public class AdminUsersControllerTests
{
    private const string AdminRole = "Admin";

    private MsSqlContainer _dbContainer = null!;
    private Respawner _respawner = null!;
    private string _connectionString = string.Empty;
    private ApiWebApplicationFactory _factory = null!;
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

        var configuration = TestConfiguration.Build(_connectionString);

        _factory = new ApiWebApplicationFactory(
            configuration: configuration,
            configureServices: services =>
            {
                services.AddTestAuthentication();
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
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task List_WithLimitOffsetAndSort_ReturnsPagedSortedUsers()
    {
        await SeedUsersAsync(
            (new ApplicationUser { UserName = "alice@example.com", Email = "alice@example.com", PhoneNumber = "100", TelegramUserId = 1 }, new[] { "Viewer" }),
            (new ApplicationUser { UserName = "bob@example.com", Email = "bob@example.com", PhoneNumber = "200", TelegramUserId = 2 }, new[] { "Manager", "Editor" }),
            (new ApplicationUser { UserName = "charlie@example.com", Email = "charlie@example.com", PhoneNumber = "300", TelegramUserId = 3 }, Array.Empty<string>()));

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/users?limit=2&offset=1&sort=-email");
        AddAdminHeaders(request);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();
        var users = JsonSerializer.Deserialize<List<UserDto>>(payload, _jsonOptions);

        users.Should().NotBeNull();
        users!.Should().HaveCount(2);
        users.Select(u => u.Email).Should().ContainInOrder("bob@example.com", "alice@example.com");
        users[0].Roles.Should().BeEquivalentTo(new[] { "Manager", "Editor" });
        users[1].Roles.Should().BeEquivalentTo(new[] { "Viewer" });
    }

    [Test]
    public async Task List_WithFilters_ReturnsOnlyMatchingUsers()
    {
        await SeedUsersAsync(
            (new ApplicationUser { UserName = "john@example.com", Email = "john@example.com", PhoneNumber = "111", TelegramUserId = 11 }, new[] { "Support" }),
            (new ApplicationUser { UserName = "jane@example.com", Email = "jane@example.com", PhoneNumber = "222", TelegramUserId = null }, new[] { "Auditor" }),
            (new ApplicationUser { UserName = "josh@example.com", Email = "josh@example.com", PhoneNumber = "333", TelegramUserId = 33 }, Array.Empty<string>()));

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
        users[0].Roles.Should().BeEquivalentTo(new[] { "Auditor" });
    }

    [Test]
    public async Task List_WithHasTelegramTrueAndTelegramSort_ReturnsOnlyTelegramUsers()
    {
        await SeedUsersAsync(
            (new ApplicationUser { UserName = "adam@example.com", Email = "adam@example.com", TelegramUserId = 44 }, new[] { "Moderator" }),
            (new ApplicationUser { UserName = "bella@example.com", Email = "bella@example.com", TelegramUserId = null }, Array.Empty<string>()),
            (new ApplicationUser { UserName = "carol@example.com", Email = "carol@example.com", TelegramUserId = 11 }, new[] { "Contributor", "Uploader" }));

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/users?hasTelegram=true&sort=-telegram");
        AddAdminHeaders(request);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();
        var users = JsonSerializer.Deserialize<List<UserDto>>(payload, _jsonOptions);

        users.Should().NotBeNull();
        users!.Select(u => u.TelegramUserId).Should().Equal(44, 11);
        users[0].Roles.Should().BeEquivalentTo(new[] { "Moderator" });
        users[1].Roles.Should().BeEquivalentTo(new[] { "Contributor", "Uploader" });
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
        await SeedUsersAsync((existing, Array.Empty<string>()));

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
        request.Headers.Add(TestAuthenticationDefaults.UserHeader, "admin");
        request.Headers.Add(TestAuthenticationDefaults.RolesHeader, AdminRole);
    }

    private async Task SeedUsersAsync(params (ApplicationUser User, IEnumerable<string> Roles)[] users)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var (user, roles) in users)
        {
            var result = await userManager.CreateAsync(user, "P@ssw0rd!");
            result.Succeeded.Should().BeTrue(result.Errors.FirstOrDefault()?.Description);

            var distinctRoles = roles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (distinctRoles.Length == 0)
            {
                continue;
            }

            foreach (var role in distinctRoles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var createRoleResult = await roleManager.CreateAsync(new IdentityRole(role));
                    createRoleResult.Succeeded.Should().BeTrue(createRoleResult.Errors.FirstOrDefault()?.Description);
                }

                var addResult = await userManager.AddToRoleAsync(user, role);
                addResult.Succeeded.Should().BeTrue(addResult.Errors.FirstOrDefault()?.Description);
            }
        }
    }

}
