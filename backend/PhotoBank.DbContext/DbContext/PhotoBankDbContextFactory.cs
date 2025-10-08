using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PhotoBank.DbContext.DbContext
{
    public class PhotoBankDbContextFactory : IDesignTimeDbContextFactory<PhotoBankDbContext>
    {
        public PhotoBankDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PhotoBankDbContext>();
            optionsBuilder.UseSqlServer(
                "Server=localhost;Database=Photobank;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=False;",
                o => o.UseNetTopologySuite());
            return new PhotoBankDbContext(optionsBuilder.Options);
        }
    }
}
