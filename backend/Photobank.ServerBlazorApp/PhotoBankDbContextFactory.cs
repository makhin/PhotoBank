using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PhotoBank.DbContext.DbContext;

namespace PhotoBank.ServerBlazorApp
{
    public class PhotoBankDbContextFactory : IDesignTimeDbContextFactory<PhotoBankDbContext>
    {
        public PhotoBankDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PhotoBankDbContext>();
            optionsBuilder.UseSqlServer("Server=STRIX;Database=Photobank;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=False;", b =>
            {
                b.MigrationsAssembly(typeof(PhotoBankDbContext).GetTypeInfo().Assembly.GetName().Name);
                b.UseNetTopologySuite();
            });

            return new PhotoBankDbContext(optionsBuilder.Options);
        }
    }
}
