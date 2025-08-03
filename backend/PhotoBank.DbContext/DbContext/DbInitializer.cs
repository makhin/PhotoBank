using Microsoft.EntityFrameworkCore;

namespace PhotoBank.DbContext.DbContext
{
    public static class DbInitializer
    {
        public static void Initialize(PhotoBankDbContext context)
        {
            context.Database.Migrate();
        }
    }
}
