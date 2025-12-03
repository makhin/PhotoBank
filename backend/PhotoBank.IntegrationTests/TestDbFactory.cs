using System;
using System.Threading.Tasks;
using PhotoBank.DbContext.DbContext;
using PhotoBank.IntegrationTests.Infra;

namespace PhotoBank.IntegrationTests;

/// <summary>
/// Manages a PostgreSQL Testcontainer for integration tests.
/// Provides database context creation with migrations applied and data cleanup via Respawner.
/// </summary>
public sealed class TestDatabaseFixture : IAsyncDisposable
{
    private readonly PostgreSqlIntegrationTestFixture _fixture = new();

    /// <summary>
    /// Gets the PostgreSQL connection string for the test database.
    /// </summary>
    public string ConnectionString => _fixture.ConnectionString;

    /// <summary>
    /// Starts the PostgreSQL container and applies migrations.
    /// Call this from [OneTimeSetUp] or test constructor.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
        _fixture.EnsureDatabaseAvailable();
    }

    /// <summary>
    /// Creates a new DbContext instance connected to the test database.
    /// </summary>
    public PhotoBankDbContext CreateContext()
    {
        return _fixture.CreatePhotoDbContext();
    }

    /// <summary>
    /// Resets the database to a clean state by deleting all data (except migration history).
    /// Call this from [SetUp] before each test.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    /// <summary>
    /// Stops the PostgreSQL container and releases resources.
    /// Call this from [OneTimeTearDown] or in a finally block.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }
}
