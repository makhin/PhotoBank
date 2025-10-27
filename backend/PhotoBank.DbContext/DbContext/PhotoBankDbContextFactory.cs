using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PhotoBank.DbContext.DbContext
{
    public class PhotoBankDbContextFactory : IDesignTimeDbContextFactory<PhotoBankDbContext>
    {
        public PhotoBankDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PhotoBankDbContext>();
            optionsBuilder.UseNpgsql(
                "Host=localhost;Database=photobank;Username=postgres;Password=postgres",
                o => o.UseNetTopologySuite());
            return new PhotoBankDbContext(optionsBuilder.Options);
        }
    }
}
