using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NUnit.Framework;
using PhotoBank.AccessControl;
using PhotoBank.DbContext.DbContext;
using Respawn;
using Testcontainers.PostgreSql;

namespace PhotoBank.IntegrationTests.Infra;

/// <summary>
/// Shared PostgreSQL test fixture that starts a Testcontainers instance, applies migrations,
/// cleans data between tests with Respawn, and builds configured <see cref="ApiWebApplicationFactory" /> instances.
/// </summary>
public sealed class PostgreSqlIntegrationTestFixture : IAsyncDisposable
{
    public const string PhotoHistoryTable = "__EFMigrationsHistory_Photo";
    public const string AccessHistoryTable = "__EFMigrationsHistory_Access";

    private PostgreSqlContainer? _container;
    private Respawner? _respawner;
    private string? _skipReason;

    public string ConnectionString { get; private set; } = string.Empty;

    public bool IsAvailable => _skipReason is null;

    public string? SkipReason => _skipReason;

    public async Task InitializeAsync()
    {
        if (_container is not null || _skipReason is not null)
        {
            return;
        }

        // Configure Npgsql to treat DateTime with Kind=Unspecified as UTC
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("kristofdetroch/postgres-postgis-pgvector:latest")
                .WithPassword("postgres")
                .Build();

            await _container.StartAsync();
        }
        catch (ArgumentException ex) when (ex.Message.Contains("Docker endpoint"))
        {
            _skipReason = "Docker not available: " + ex.Message;
            return;
        }
        catch (DockerUnavailableException ex)
        {
            _skipReason = "Docker not available: " + ex.Message;
            return;
        }

        ConnectionString = _container.GetConnectionString();

        await ApplyMigrationsAsync();
        await ConfigureRespawnerAsync();
    }

    public void EnsureDatabaseAvailable()
    {
        if (_skipReason is not null)
        {
            Assert.Ignore(_skipReason);
        }
    }

    public async Task ResetDatabaseAsync()
    {
        EnsureDatabaseAvailable();

        if (_respawner is null)
        {
            throw new InvalidOperationException("Respawner was not initialized.");
        }

        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        await _respawner.ResetAsync(conn);
    }

    public IConfiguration CreateConfiguration(IReadOnlyDictionary<string, string?>? overrides = null)
    {
        EnsureDatabaseAvailable();

        return new ConfigurationBuilder()
            .AddInMemoryCollection(TestConfiguration.Build(ConnectionString, overrides))
            .Build();
    }

    public ApiWebApplicationFactory CreateApiFactory(
        IReadOnlyDictionary<string, string?>? configurationOverrides = null,
        Action<IServiceCollection>? configureServices = null,
        string environment = "Development")
    {
        EnsureDatabaseAvailable();

        var configuration = TestConfiguration.Build(ConnectionString, configurationOverrides);
        return new ApiWebApplicationFactory(configuration, configureServices, environment);
    }

    public PhotoBankDbContext CreatePhotoDbContext(bool enableLogging = true)
    {
        EnsureDatabaseAvailable();

        var options = BuildPhotoDbOptions(enableLogging);
        return new PhotoBankDbContext(options);
    }

    public AccessControlDbContext CreateAccessControlDbContext(bool enableLogging = true)
    {
        EnsureDatabaseAvailable();

        var options = BuildAccessDbOptions(enableLogging);
        return new AccessControlDbContext(options);
    }

    public void AddNpgsqlDbContexts(IServiceCollection services)
    {
        EnsureDatabaseAvailable();

        services.AddDbContext<PhotoBankDbContext>(options => ConfigurePhotoDbOptions(options));
        services.AddDbContext<AccessControlDbContext>(options => ConfigureAccessDbOptions(options));
    }

    private async Task ApplyMigrationsAsync()
    {
        await using (var photoDb = new PhotoBankDbContext(BuildPhotoDbOptions(enableLogging: false)))
        {
            await photoDb.Database.MigrateAsync();
        }

        await using (var accessDb = new AccessControlDbContext(BuildAccessDbOptions(enableLogging: false)))
        {
            await accessDb.Database.MigrateAsync();
        }
    }

    private async Task ConfigureRespawnerAsync()
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            TablesToIgnore = new[]
            {
                new Respawn.Graph.Table(PhotoHistoryTable),
                new Respawn.Graph.Table(AccessHistoryTable)
            }
        });
    }

    private DbContextOptions<PhotoBankDbContext> BuildPhotoDbOptions(bool enableLogging)
    {
        var builder = new DbContextOptionsBuilder<PhotoBankDbContext>();
        ConfigurePhotoDbOptions(builder);

        if (enableLogging)
        {
            builder.EnableSensitiveDataLogging();
            builder.EnableDetailedErrors();
        }

        return builder.Options;
    }

    private DbContextOptions<AccessControlDbContext> BuildAccessDbOptions(bool enableLogging)
    {
        var builder = new DbContextOptionsBuilder<AccessControlDbContext>();
        ConfigureAccessDbOptions(builder);

        if (enableLogging)
        {
            builder.EnableSensitiveDataLogging();
            builder.EnableDetailedErrors();
        }

        return builder.Options;
    }

    private void ConfigurePhotoDbOptions(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w => w.Log(RelationalEventId.PendingModelChangesWarning));
        optionsBuilder.UseNpgsql(ConnectionString, npgsql =>
        {
            npgsql.MigrationsAssembly(typeof(PhotoBankDbContext).Assembly.GetName().Name);
            npgsql.MigrationsHistoryTable(PhotoHistoryTable);
            npgsql.UseVector();
            npgsql.UseNetTopologySuite();
        });
    }

    private void ConfigureAccessDbOptions(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w => w.Log(RelationalEventId.PendingModelChangesWarning));
        optionsBuilder.UseNpgsql(ConnectionString, npgsql =>
        {
            npgsql.MigrationsAssembly(typeof(AccessControlDbContext).Assembly.GetName().Name);
            npgsql.MigrationsHistoryTable(AccessHistoryTable);
            npgsql.UseVector();
            npgsql.UseNetTopologySuite();
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}
