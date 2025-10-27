using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using Testcontainers.PostgreSql;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.ViewModel.Dto;
using Respawn;
using PhotoBank.IntegrationTests.Infra;

namespace PhotoBank.IntegrationTests.Auth;

[TestFixture]
public class TelegramSubscriptionsTests
{
    private const string ServiceKey = "integration-telegram-key";

    private PostgreSqlContainer _dbContainer = null!;
    private Respawner _respawner = null!;
    private string _connectionString = string.Empty;
    private ApiWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        // Configure Npgsql to treat DateTime with Kind=Unspecified as UTC
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        try
        {
            _dbContainer = new PostgreSqlBuilder()
                .WithPassword("postgres")
                .Build();
            await _dbContainer.StartAsync();
        }
        catch (ArgumentException ex) when (ex.Message.Contains("Docker endpoint"))
        {
            Assert.Ignore("Docker not available: " + ex.Message);
        }

        _connectionString = _dbContainer.GetConnectionString();

        var options = new DbContextOptionsBuilder<PhotoBankDbContext>()
            .UseNpgsql(_connectionString, builder =>
            {
                builder.MigrationsAssembly(typeof(PhotoBankDbContext).Assembly.GetName().Name);
                builder.UseNetTopologySuite();
            })
            .Options;

        await using (var db = new PhotoBankDbContext(options))
        {
            await db.Database.MigrateAsync();
        }

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
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

        await using (var conn = new NpgsqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            await _respawner.ResetAsync(conn);
        }

        var configuration = TestConfiguration.Build(
            _connectionString,
            new Dictionary<string, string?>
            {
                ["Auth:Telegram:ServiceKey"] = ServiceKey
            });

        _factory = new ApiWebApplicationFactory(configuration: configuration);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task GetTelegramSubscriptions_WithoutServiceKey_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/telegram/subscriptions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetTelegramSubscriptions_WithServiceKey_ReturnsSubscriptions()
    {
        await SeedUsersAsync();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/telegram/subscriptions");
        request.Headers.Add("X-Service-Key", ServiceKey);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<TelegramSubscriptionDto>>(payload, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(new[]
        {
            new TelegramSubscriptionDto
            {
                TelegramUserId = "9007199254740993",
                TelegramSendTimeUtc = TimeSpan.FromHours(8)
            },
            new TelegramSubscriptionDto
            {
                TelegramUserId = "987654321",
                TelegramSendTimeUtc = TimeSpan.FromHours(9)
            }
        }, options => options.WithStrictOrdering());
    }

    private async Task SeedUsersAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var alice = new ApplicationUser
        {
            UserName = "alice@example.com",
            Email = "alice@example.com",
            TelegramUserId = 9_007_199_254_740_993,
            TelegramSendTimeUtc = TimeSpan.FromHours(8)
        };

        var bob = new ApplicationUser
        {
            UserName = "bob@example.com",
            Email = "bob@example.com",
            TelegramUserId = 987654321,
            TelegramSendTimeUtc = TimeSpan.FromHours(9)
        };

        var charlie = new ApplicationUser
        {
            UserName = "charlie@example.com",
            Email = "charlie@example.com",
            TelegramUserId = 555,
            TelegramSendTimeUtc = null
        };

        var dana = new ApplicationUser
        {
            UserName = "dana@example.com",
            Email = "dana@example.com",
            TelegramUserId = null,
            TelegramSendTimeUtc = TimeSpan.FromHours(10)
        };

        var result1 = await userManager.CreateAsync(alice);
        result1.Succeeded.Should().BeTrue();

        var result2 = await userManager.CreateAsync(bob);
        result2.Succeeded.Should().BeTrue();

        var result3 = await userManager.CreateAsync(charlie);
        result3.Succeeded.Should().BeTrue();

        var result4 = await userManager.CreateAsync(dana);
        result4.Succeeded.Should().BeTrue();
    }

}
