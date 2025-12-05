using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Minio;
using Moq;
using NUnit.Framework;
using PhotoBank.AccessControl;
using PhotoBank.Api.Controllers;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.DependencyInjection;
using PhotoBank.IntegrationTests.Infra;
using PhotoBank.Services.Api;
using PhotoBank.ViewModel.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoBank.IntegrationTests;

[TestFixture]
[Category("Integration")]
public class GetPersonsIntegrationTests
{
    private readonly PostgreSqlIntegrationTestFixture _fixture = new();
    private IConfiguration _config = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        await _fixture.InitializeAsync();
        _fixture.EnsureDatabaseAvailable();

        _config = _fixture.CreateConfiguration(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "secret"
        });
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
    }

    [Test]
    public async Task GetAllAsync_NonAdminUser_ReturnsOnlyAllowedPersons()
    {
        var seed = await SeedPersonsAsync();

        await using var provider = BuildProvider(seed.AllowedGroupId);
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IPhotoService>();
        var controller = new PersonsController(service);

        var actionResult = await controller.GetAllAsync();

        var okResult = actionResult.Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
        okResult.Should().NotBeNull();

        var persons = okResult!.Value.Should().BeAssignableTo<IEnumerable<PersonDto>>().Subject.ToList();
        persons.Should().HaveCount(1);
        persons[0].Id.Should().Be(seed.AllowedPersonId);
    }

    private async Task<PersonSeedResult> SeedPersonsAsync()
    {
        await using var db = CreateDbContext();

        var allowedGroup = new PersonGroup { Name = "Allowed" };
        var blockedGroup = new PersonGroup { Name = "Blocked" };

        var allowedPerson = new Person { Name = "Alice", PersonGroups = new List<PersonGroup> { allowedGroup } };
        var blockedPerson = new Person { Name = "Bob", PersonGroups = new List<PersonGroup> { blockedGroup } };
        var ungroupedPerson = new Person { Name = "Charlie", PersonGroups = new List<PersonGroup>() };

        db.PersonGroups.AddRange(allowedGroup, blockedGroup);
        db.Persons.AddRange(allowedPerson, blockedPerson, ungroupedPerson);

        await db.SaveChangesAsync();

        return new PersonSeedResult(allowedGroup.Id, allowedPerson.Id, blockedPerson.Id, ungroupedPerson.Id);
    }

    private PhotoBankDbContext CreateDbContext()
    {
        return _fixture.CreatePhotoDbContext(enableLogging: false);
    }

    private ServiceProvider BuildProvider(params int[] allowedPersonGroupIds)
    {
        var services = new ServiceCollection();
        _fixture.AddNpgsqlDbContexts(services);

        services.AddPhotobankCore(_config);
        services.AddPhotobankApi(_config);
        services.AddPhotobankCors();

        services.RemoveAll<ICurrentUserAccessor>();
        services.AddScoped<ICurrentUserAccessor>(_ => new TestCurrentUserAccessor(new NonAdminTestUser("user", allowedPersonGroupIds)));

        services.AddLogging();
        services.AddSingleton<IMinioClient>(Mock.Of<IMinioClient>());

        return services.BuildServiceProvider();
    }

    private sealed record PersonSeedResult(int AllowedGroupId, int AllowedPersonId, int BlockedPersonId, int UngroupedPersonId);

    private sealed class TestCurrentUserAccessor : ICurrentUserAccessor
    {
        private readonly ICurrentUser _user;

        public TestCurrentUserAccessor(ICurrentUser user)
        {
            _user = user;
        }

        public ValueTask<ICurrentUser> GetCurrentUserAsync(CancellationToken ct = default)
            => ValueTask.FromResult(_user);

        public ICurrentUser CurrentUser => _user;
    }

    private sealed class NonAdminTestUser : ICurrentUser
    {
        public NonAdminTestUser(string userId, IEnumerable<int> allowedPersonGroupIds)
        {
            UserId = userId;
            AllowedPersonGroupIds = new HashSet<int>(allowedPersonGroupIds);
        }

        public string UserId { get; }
        public bool IsAdmin => false;
        public IReadOnlySet<int> AllowedStorageIds { get; } = new HashSet<int>();
        public IReadOnlySet<int> AllowedPersonGroupIds { get; }
        public IReadOnlyList<(DateOnly From, DateOnly To)> AllowedDateRanges { get; } = Array.Empty<(DateOnly, DateOnly)>();
        public bool CanSeeNsfw => false;
    }
}

