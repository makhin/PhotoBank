using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;

namespace PhotoBank.Repositories
{
    public interface IRepository<TTable> where TTable : class, IEntityBase, new()
    {
        IQueryable<TTable> GetByCondition(Expression<Func<TTable, bool>> predicate);
        IQueryable<TTable> GetAll();
        Task<TTable> GetAsync(int id, Func<IQueryable<TTable>, IQueryable<TTable>> queryable);
        TTable Get(int id, Func<IQueryable<TTable>, IQueryable<TTable>> queryable);
        Task<TTable> GetAsync(int id);
        TTable Get(int id);
        Task<TTable> InsertAsync(TTable entity);
        Task InsertRangeAsync(List<TTable> entities);
        Task<TTable> UpdateAsync(TTable entity);
        Task<int> UpdateAsync(TTable entity, params Expression<Func<TTable, object>>[] properties);
        Task<int> DeleteAsync(int id);
        void Attach(TTable entity);
    }

    public class Repository<TTable> : IRepository<TTable> where TTable : class, IEntityBase, new()
    {
        private readonly PhotoBankDbContext _context;
        private readonly DbSet<TTable> _entities;

        public Repository(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<PhotoBankDbContext>();
            _context = context;
            _entities = context.Set<TTable>();
        }

        public IQueryable<TTable> GetAll() => _entities.AsNoTracking();

        public IQueryable<TTable> GetByCondition(Expression<Func<TTable, bool>> predicate)
        {
            return GetAll().Where(predicate);
        }

        public async Task<TTable> GetAsync(int id, Func<IQueryable<TTable>, IQueryable<TTable>> queryable)
        {
            var query = queryable(GetAll());
            return await query.SingleOrDefaultAsync(a => a.Id == id);
        }

        public TTable Get(int id, Func<IQueryable<TTable>, IQueryable<TTable>> queryable)
        {
            var query = queryable(GetAll());
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

        public async Task InsertRangeAsync(List<TTable> entities)
        {
            try
            {
                await _context.AddRangeAsync(entities);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException exception)
            {
                Debug.WriteLine("An exception occurred: {0}, {1}", exception.InnerException, exception.Message);
                throw new Exception("An error occurred; new record not saved");
            }
        }

        public async Task<TTable> InsertAsync(TTable entity)
        {
            try
            {
                await _context.AddAsync(entity);
                await _context.SaveChangesAsync();
                return entity;
            }
            catch (DbUpdateException exception)
            {
                Debug.WriteLine("An exception occurred: {0}, {1}", exception.InnerException, exception.Message);
                throw new Exception("An error occurred; new record not saved");
            }
        }
        public async Task<TTable> UpdateAsync(TTable entity)
        {
            var recordExists = await _entities.AnyAsync(a => a.Id == entity.Id);

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

        public async Task<int> UpdateAsync(TTable entity, Expression<Func<TTable, object>>[] properties)
        {
            var entry = _entities.Attach(entity);
            entry.State = EntityState.Unchanged;

            foreach (var property in properties)
            {
                string propertyName = property.Body switch
                {
                    MemberExpression member => member.Member.Name,
                    UnaryExpression unary when unary.Operand is MemberExpression member => member.Member.Name,
                    _ => throw new InvalidOperationException("Invalid property expression")
                };

                entry.Property(propertyName).IsModified = true;
            }

            return await _context.SaveChangesAsync();
        }

        public async Task<int> DeleteAsync(int id)
        {
            var entity = await _entities.FindAsync(id);

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

        public void Attach(TTable entity)
        {
            if (_context.Entry(entity).State == EntityState.Detached)
            {
                _entities.Attach(entity);
            }
        }
    }
}
