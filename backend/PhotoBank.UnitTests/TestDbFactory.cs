using System;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.DbContext;

namespace PhotoBank.UnitTests;

public static class TestDbFactory
{
    public static PhotoBankDbContext CreateInMemory()
    {
        var options = new DbContextOptionsBuilder<PhotoBankDbContext>()
            .UseInMemoryDatabase($"pb-tests-{Guid.NewGuid()}")
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors()
            .Options;

        var ctx = new PhotoBankDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }
}
