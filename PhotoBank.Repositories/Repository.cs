using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;

namespace PhotoBank.Repositories
{
    public interface IRepository<TTable> where TTable : class, IEntityBase
    {
        IQueryable<TTable> GetByCondition(Expression<Func<TTable, bool>> predicate);
        IQueryable<TTable> GetAll();
        Task<TTable> GetAsync(int id, Func<IQueryable<TTable>, IQueryable<TTable>> queryable);
        TTable Get(int id, Func<IQueryable<TTable>, IQueryable<TTable>> queryable);
        Task<TTable> GetAsync(int id);
        TTable Get(int id);
        Task<TTable> InsertAsync(TTable entity);
        Task<TTable> UpdateAsync(TTable entity);
        Task<int> DeleteAsync(int id);
    }

    public class Repository<TTable> : IRepository<TTable> where TTable : class, IEntityBase
    {
        private readonly PhotoBankDbContext _context;
        private readonly DbSet<TTable> _entities;

        public Repository(PhotoBankDbContext context)
        {
            this._context = context;
            _entities = context.Set<TTable>();
        }

        public IQueryable<TTable> GetByCondition(Expression<Func<TTable, bool>> predicate)
        {
            return _entities.Where(predicate);
        }

        public IQueryable<TTable> GetAll()
        {
            return _entities;
        }

        public async Task<TTable> GetAsync(int id, Func<IQueryable<TTable>, IQueryable<TTable>> queryable)
        {
            IQueryable<TTable> query = _entities.AsNoTracking();
            query = queryable(query);
            return await query.SingleOrDefaultAsync(a => a.Id == id);
        }

        public TTable Get(int id, Func<IQueryable<TTable>, IQueryable<TTable>> queryable)
        {
            IQueryable<TTable> query = _entities.AsNoTracking();
            query = queryable(query);
            return  query.SingleOrDefault(a => a.Id == id);
        }

        public async Task<TTable> GetAsync(int id)
        {
            return await _entities.FindAsync(id);
        }

        public TTable Get(int id)
        {
            return _entities.Find(id);
        }

        public async Task<TTable> InsertAsync(TTable entity)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.AddAsync(entity);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return entity;
            }
            catch (DbUpdateException exception)
            {
                await transaction.RollbackAsync();
                Debug.WriteLine("An exception occurred: {0}, {1}", exception.InnerException, exception.Message);
                throw new Exception("An error occurred; new record not saved");
            }
        }
        public async Task<TTable> UpdateAsync(TTable entity)
        {
            bool recordExists = _entities.Any(a => a.Id == entity.Id);

            if (!recordExists)
            {
                throw new Exception("An error occurred; record not found");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _entities.Update(entity);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return entity;
            }
            catch (DbUpdateException exception)
            {
                await transaction.RollbackAsync();
                Debug.WriteLine("An exception occurred: {0}, {1}", exception.InnerException, exception.Message);
                throw new Exception("An error occurred; record not updated");
            }
        }

        public async Task<int> DeleteAsync(int id)
        {
            TTable entity = await _entities.AsNoTracking().SingleOrDefaultAsync(m => m.Id == id);

            if (entity == null)
            {
                throw new Exception("Record not found; not deleted");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _entities.Remove(entity);
                var i = await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return i;
            }
            catch (DbUpdateException exception)
            {
                await transaction.RollbackAsync();
                Debug.WriteLine("An exception occurred: {0}, {1}", exception.InnerException, exception.Message);
                throw new Exception("An error occurred; not deleted");
            }
        }
    }
}
