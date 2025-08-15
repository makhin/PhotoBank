using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.Models;
using System.Linq;

namespace PhotoBank.DbContext.DbContext;

public static class CompiledQueries
{
    public static readonly Func<PhotoBankDbContext, int, Task<Photo?>> PhotoById =
        EF.CompileAsyncQuery((PhotoBankDbContext db, int id) =>
            db.Photos.AsNoTracking()
                .Include(p => p.PhotoTags).ThenInclude(t => t.Tag)
                .Include(p => p.Faces).ThenInclude(f => f.Person)
                .Include(p => p.Captions)
                .FirstOrDefault(p => p.Id == id));
}
