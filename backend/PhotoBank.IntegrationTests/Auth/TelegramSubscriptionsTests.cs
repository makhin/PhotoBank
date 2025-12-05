using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.IntegrationTests.Infra;
using PhotoBank.ViewModel.Dto;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhotoBank.IntegrationTests.Auth;

[TestFixture]
[Category("Integration")]
public class TelegramSubscriptionsTests
{
    private const string ServiceKey = "integration-telegram-key";

    private readonly PostgreSqlIntegrationTestFixture _fixture = new();
    private ApiWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        await _fixture.InitializeAsync();
        _fixture.EnsureDatabaseAvailable();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _fixture.DisposeAsync();
    }

    [SetUp]
    public async Task Setup()
    {
        await _fixture.ResetDatabaseAsync();

        var configurationOverrides = new Dictionary<string, string?>
        {
            ["Auth:Telegram:ServiceKey"] = ServiceKey
        };

        _factory = _fixture.CreateApiFactory(configurationOverrides);
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
