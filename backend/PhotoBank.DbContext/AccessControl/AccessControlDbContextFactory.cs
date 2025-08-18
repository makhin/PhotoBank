using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PhotoBank.AccessControl;

public class AccessControlDbContextFactory : IDesignTimeDbContextFactory<AccessControlDbContext>
{
    public AccessControlDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AccessControlDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=Photobank;Trusted_Connection=True;Encrypt=False;");
        return new AccessControlDbContext(optionsBuilder.Options);
    }
}
