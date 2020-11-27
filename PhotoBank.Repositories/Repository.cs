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
        Task<TTable> Get(int id, Func<IQueryable<TTable>, IQueryable<TTable>> queryable);
        Task<TTable> Insert(TTable entity);
        Task<TTable> Update(TTable entity);
        Task<int> Delete(int id);
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

        public async Task<TTable> Get(int id, Func<IQueryable<TTable>, IQueryable<TTable>> queryable)
        {
            IQueryable<TTable> query = _entities;
            query = queryable(query);
            return await query.SingleOrDefaultAsync(a => a.Id == id);
        }

        public async Task<TTable> Insert(TTable entity)
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
        public async Task<TTable> Update(TTable entity)
        {
            bool recordExists = _entities.Any(a => a.Id == entity.Id);

            if (!recordExists)
            {
                throw new Exception("An error occurred; record not found");
            }

            try
            {
                _entities.Update(entity);
                await _context.SaveChangesAsync();
                return entity;
            }
            catch (DbUpdateException exception)
            {
                Debug.WriteLine("An exception occurred: {0}, {1}", exception.InnerException, exception.Message);
                throw new Exception("An error occurred; record not updated");
            }
        }

        public async Task<int> Delete(int id)
        {
            TTable entity = await _entities.AsNoTracking().SingleOrDefaultAsync(m => m.Id == id);

            if (entity == null)
            {
                throw new Exception("Record not found; not deleted");
            }

            try
            {
                _entities.Remove(entity);
                return await _context.SaveChangesAsync();
            }
            catch (DbUpdateException exception)
            {
                Debug.WriteLine("An exception occurred: {0}, {1}", exception.InnerException, exception.Message);
                throw new Exception("An error occurred; not deleted");
            }
        }
    }
}
