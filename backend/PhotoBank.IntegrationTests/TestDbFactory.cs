using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using PhotoBank.DbContext.DbContext;
using Respawn;
using Testcontainers.PostgreSql;

namespace PhotoBank.IntegrationTests;

/// <summary>
/// Manages a PostgreSQL Testcontainer for integration tests.
/// Provides database context creation with migrations applied and data cleanup via Respawner.
/// </summary>
public sealed class TestDatabaseFixture : IAsyncDisposable
{
    private PostgreSqlContainer? _container;
    private Respawner? _respawner;
    private string _connectionString = string.Empty;

    /// <summary>
    /// Gets the PostgreSQL connection string for the test database.
    /// </summary>
    public string ConnectionString => _connectionString;

    /// <summary>
    /// Starts the PostgreSQL container and applies migrations.
    /// Call this from [OneTimeSetUp] or test constructor.
    /// </summary>
    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgis/postgis:16-3.4")
            .WithPassword("postgres")
            .Build();

        await _container.StartAsync();

        _connectionString = _container.GetConnectionString();

        // Apply PhotoBankDbContext migrations
        var photoDbOptionsBuilder = new DbContextOptionsBuilder<PhotoBankDbContext>();
        photoDbOptionsBuilder.ConfigureWarnings(w =>
            w.Log(RelationalEventId.PendingModelChangesWarning));
        photoDbOptionsBuilder.UseNpgsql(_connectionString, builder =>
        {
            builder.MigrationsAssembly(typeof(PhotoBankDbContext).Assembly.GetName().Name);
            builder.UseNetTopologySuite();
        });

        await using (var photoDb = new PhotoBankDbContext(photoDbOptionsBuilder.Options))
        {
            await photoDb.Database.MigrateAsync();
        }

        // Initialize Respawner for data cleanup between tests
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

    /// <summary>
    /// Creates a new DbContext instance connected to the test database.
    /// </summary>
    public PhotoBankDbContext CreateContext()
    {
        if (string.IsNullOrEmpty(_connectionString))
        {
            throw new InvalidOperationException("Database not initialized. Call InitializeAsync() first.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<PhotoBankDbContext>();
        optionsBuilder.UseNpgsql(_connectionString, builder =>
        {
            builder.MigrationsAssembly(typeof(PhotoBankDbContext).Assembly.GetName().Name);
            builder.UseNetTopologySuite();
        });
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableDetailedErrors();

        return new PhotoBankDbContext(optionsBuilder.Options);
    }

    /// <summary>
    /// Resets the database to a clean state by deleting all data (except migration history).
    /// Call this from [SetUp] before each test.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        if (_respawner == null)
        {
            throw new InvalidOperationException("Respawner not initialized. Call InitializeAsync() first.");
        }

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await _respawner.ResetAsync(conn);
    }

    /// <summary>
    /// Stops the PostgreSQL container and releases resources.
    /// Call this from [OneTimeTearDown] or in a finally block.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }
}
