using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PhotoBank.DbContext.DbContext;

namespace PhotoBank.UnitTests;

/// <summary>
/// Factory for creating in-memory database contexts for unit tests.
///
/// WARNING: InMemory provider differs from production PostgreSQL:
/// - EnableNullChecks(false): Null constraint violations won't be caught
/// - TransactionIgnoredWarning ignored: Transactions don't actually rollback
///
/// Use integration tests (PhotoBank.IntegrationTests) with real PostgreSQL for:
/// - Testing transactional rollback behavior
/// - Testing required field/null constraint violations
/// - Testing database-specific features (indexes, constraints, etc.)
///
/// Unit tests here are for fast, isolated testing of business logic only.
/// </summary>
public static class TestDbFactory
{
    public static PhotoBankDbContext CreateInMemory()
    {
        var options = new DbContextOptionsBuilder<PhotoBankDbContext>()
            // NOTE: Null checks disabled to simplify test data setup.
            // Test required fields in integration tests with real PostgreSQL.
            .UseInMemoryDatabase($"pb-tests-{Guid.NewGuid()}", b => b.EnableNullChecks(false))
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors()
            // NOTE: InMemory doesn't support real transactions - they're no-ops.
            // Test transactional rollback in integration tests with real PostgreSQL.
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var ctx = new PhotoBankDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }
}
