using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.DbContext;
using Testcontainers.MsSql;
using Xunit;

public class MsSqlContainerTests : IAsyncLifetime
{
    private readonly MsSqlContainer _msSqlContainer =
        new MsSqlBuilder().WithPassword("yourStrong(!)Password").Build();

    public async Task InitializeAsync() => await _msSqlContainer.StartAsync();

    public async Task DisposeAsync() => await _msSqlContainer.DisposeAsync();

    [Fact]
    public async Task Should_connect_and_apply_migrations()
    {
        var options = new DbContextOptionsBuilder<PhotoBankDbContext>()
            .UseSqlServer(_msSqlContainer.GetConnectionString())
            .Options;

        await using var context = new PhotoBankDbContext(options);
        await context.Database.MigrateAsync();

        await using var connection = new SqlConnection(_msSqlContainer.GetConnectionString());
        await connection.OpenAsync();

        using var command = new SqlCommand("SELECT 1", connection);
        var result = (int)await command.ExecuteScalarAsync();

        result.Should().Be(1);
    }
}
