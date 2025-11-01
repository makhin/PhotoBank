using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace PhotoBank.DbContext.DbContext
{
    public class PhotoBankDbContextFactory : IDesignTimeDbContextFactory<PhotoBankDbContext>
    {
        public PhotoBankDbContext CreateDbContext(string[] args)
        {
            // Build configuration to read from appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "PhotoBank.Api"))
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Host=localhost;Database=photobank;Username=postgres;Password=postgres"; // fallback

            var optionsBuilder = new DbContextOptionsBuilder<PhotoBankDbContext>();
            optionsBuilder.UseNpgsql(connectionString, o => o.UseNetTopologySuite());
            return new PhotoBankDbContext(optionsBuilder.Options);
        }
    }
}
