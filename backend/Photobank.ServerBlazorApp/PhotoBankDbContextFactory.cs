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
            optionsBuilder.UseNpgsql("Host=localhost;Database=photobank;Username=postgres;Password=postgres", b =>
            {
                b.MigrationsAssembly(typeof(PhotoBankDbContext).GetTypeInfo().Assembly.GetName().Name);
                b.UseNetTopologySuite();
                b.UseVector();
            });

            return new PhotoBankDbContext(optionsBuilder.Options);
        }
    }
}
