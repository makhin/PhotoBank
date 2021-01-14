using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;

namespace PhotoBank.Repositories
{
    public interface ISimpleRepository 
    {
        Task InsertRangeAsync(List<FaceToFace> entities);
    }

    public class SimpleRepository : ISimpleRepository
    {
        private readonly PhotoBankDbContext _context;

        public SimpleRepository(PhotoBankDbContext context)
        {
            _context = context;
            _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public async Task InsertRangeAsync(List<FaceToFace> entities)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var entity in entities)
                {
                    await _context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO FaceToFaces VALUES({entity.Face1Id},{entity.Face2Id},{entity.Distance})");
                }
                await transaction.CommitAsync();
            }
            catch (DbUpdateException exception)
            {
                await transaction.RollbackAsync();
                Debug.WriteLine("An exception occurred: {0}, {1}", exception.InnerException, exception.Message);
                throw new Exception("An error occurred; new record not saved");
            }
        }
    }
}
