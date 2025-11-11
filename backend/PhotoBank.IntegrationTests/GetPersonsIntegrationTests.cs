using DotNet.Testcontainers.Builders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Minio;
using Moq;
using Npgsql;
using NUnit.Framework;
using PhotoBank.AccessControl;
using PhotoBank.Api.Controllers;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.DependencyInjection;
using PhotoBank.Services.Api;
using PhotoBank.ViewModel.Dto;
using Respawn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;

namespace PhotoBank.IntegrationTests;

[TestFixture]
[Category("Integration")]
public class GetPersonsIntegrationTests
{
    private PostgreSqlContainer _dbContainer = null!;
    private Respawner _respawner = null!;
    private IConfiguration _config = null!;
    private string _connectionString = string.Empty;
    private const string PhotoHistory = "__EFMigrationsHistory_Photo", AccessHistory = "__EFMigrationsHistory_Access";

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        // Configure Npgsql to treat DateTime with Kind=Unspecified as UTC
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        try
        {
            _dbContainer = new PostgreSqlBuilder()
                .WithImage("postgis/postgis:16-3.4")
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

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "issuer",
                ["Jwt:Audience"] = "audience",
                ["Jwt:Key"] = "secret",
                ["ConnectionStrings:DefaultConnection"] = _connectionString
            })
            .Build();

        // Prepare DbContext options like in TelegramSubscriptionsTests
        var photoDbOptionsBuilder = new DbContextOptionsBuilder<PhotoBankDbContext>();
        photoDbOptionsBuilder.ConfigureWarnings(w => w.Log(RelationalEventId.PendingModelChangesWarning));
        photoDbOptionsBuilder.UseNpgsql(_connectionString, builder =>
        {
            builder.MigrationsAssembly(typeof(PhotoBankDbContext).Assembly.GetName().Name);
            builder.MigrationsHistoryTable(PhotoHistory);
            builder.UseNetTopologySuite();
        });
        var photoDbOptions = photoDbOptionsBuilder.Options;
        await using (var photoDb = new PhotoBankDbContext(photoDbOptions))
            await photoDb.Database.MigrateAsync();

        var accessDbOptionsBuilder = new DbContextOptionsBuilder<AccessControlDbContext>();
        accessDbOptionsBuilder.ConfigureWarnings(w => w.Log(RelationalEventId.PendingModelChangesWarning));
        accessDbOptionsBuilder.UseNpgsql(_connectionString, builder =>
        {
            builder.MigrationsAssembly(typeof(AccessControlDbContext).Assembly.GetName().Name);
            builder.MigrationsHistoryTable(AccessHistory);
            builder.UseNetTopologySuite();
        });
        var accessDbOptions = accessDbOptionsBuilder.Options;
        await using (var accessDb = new AccessControlDbContext(accessDbOptions))
            await accessDb.Database.MigrateAsync();

        var services = new ServiceCollection();
        services.AddDbContext<PhotoBankDbContext>(options =>
            options.UseNpgsql(_connectionString, builder =>
            {
                builder.MigrationsAssembly(typeof(PhotoBankDbContext).Assembly.GetName().Name);
                builder.MigrationsHistoryTable(PhotoHistory);
                builder.UseNetTopologySuite();
            }));
        services.AddDbContext<AccessControlDbContext>(options =>
            options.UseNpgsql(_connectionString, builder =>
            {
                builder.MigrationsAssembly(typeof(AccessControlDbContext).Assembly.GetName().Name);
                builder.MigrationsHistoryTable(AccessHistory);
                builder.UseNetTopologySuite();
            }));
        services.AddPhotobankCore(_config);
        services.AddPhotobankApi(_config);
        services.AddPhotobankCors();

        services.RemoveAll<ICurrentUserAccessor>();
        services.AddScoped<ICurrentUserAccessor>(_ => new TestCurrentUserAccessor(new DummyCurrentUser()));
        services.AddLogging();
        services.AddSingleton<IMinioClient>(Mock.Of<IMinioClient>());

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            TablesToIgnore = new[]
            {
                new Respawn.Graph.Table(PhotoHistory),
                new Respawn.Graph.Table(AccessHistory)
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
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await _respawner.ResetAsync(conn);
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
        var options = new DbContextOptionsBuilder<PhotoBankDbContext>()
            .UseNpgsql(_connectionString, builder =>
            {
                builder.MigrationsAssembly(typeof(PhotoBankDbContext).Assembly.GetName().Name);
                builder.UseNetTopologySuite();
            })
            .Options;
        return new PhotoBankDbContext(options);
    }

    private ServiceProvider BuildProvider(params int[] allowedPersonGroupIds)
    {
        var services = new ServiceCollection();
        services.AddDbContext<PhotoBankDbContext>(options =>
            options.UseNpgsql(_connectionString, builder =>
            {
                builder.MigrationsAssembly(typeof(PhotoBankDbContext).Assembly.GetName().Name);
                builder.MigrationsHistoryTable(PhotoHistory);
                builder.UseNetTopologySuite();
            }));

        services.AddDbContext<AccessControlDbContext>(options =>
            options.UseNpgsql(_connectionString, builder =>
            {      
                builder.MigrationsAssembly(typeof(AccessControlDbContext).Assembly.GetName().Name);
                builder.MigrationsHistoryTable(AccessHistory);
                builder.UseNetTopologySuite();
            }));

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

