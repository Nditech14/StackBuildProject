using Microsoft.EntityFrameworkCore;
using StackBuld.Domain.Entities;
using StackBuld.Infrastructure.Data;
using StackBuld.Infrastructure.Repositories.OrderRepo.Abstraction;
using System.Linq.Expressions;

namespace StackBuld.Infrastructure.Repositories.OrderRepo.Implementation
{
    public class Repository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly ECommerceDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(ECommerceDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task<T?> GetByIdAsync(Guid id, bool trackChanges = false)
        {
            var query = _dbSet.AsQueryable();

            if (!trackChanges)
                query = query.AsNoTracking();

            return await query.FirstOrDefaultAsync(e => e.Id == id);
        }

        public virtual async Task<List<T>> GetAllAsync(bool trackChanges = false)
        {
            var query = _dbSet.AsQueryable();

            if (!trackChanges)
                query = query.AsNoTracking();

            return await query.ToListAsync();
        }

        public virtual async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, bool trackChanges = false)
        {
            var query = _dbSet.AsQueryable();

            if (!trackChanges)
                query = query.AsNoTracking();

            return await query.Where(predicate).ToListAsync();
        }

        public virtual async Task<T?> FindFirstAsync(Expression<Func<T, bool>> predicate, bool trackChanges = false)
        {
            var query = _dbSet.AsQueryable();

            if (!trackChanges)
                query = query.AsNoTracking();

            return await query.FirstOrDefaultAsync(predicate);
        }

        public virtual async Task<(List<T> Items, int TotalCount)> GetPagedAsync(
            int page,
            int pageSize,
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            bool trackChanges = false)
        {
            var query = _dbSet.AsQueryable();

            if (!trackChanges)
                query = query.AsNoTracking();

            if (filter != null)
                query = query.Where(filter);

            var totalCount = await query.CountAsync();

            if (orderBy != null)
                query = orderBy(query);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public virtual async Task<T> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            return entity;
        }

        public virtual async Task<List<T>> AddRangeAsync(List<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
            return entities;
        }

        public virtual void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public virtual void UpdateRange(List<T> entities)
        {
            _dbSet.UpdateRange(entities);
        }

        public virtual void Remove(T entity)
        {
            _dbSet.Remove(entity);
        }

        public virtual void RemoveRange(List<T> entities)
        {
            _dbSet.RemoveRange(entities);
        }

        public virtual async Task<bool> ExistsAsync(Guid id)
        {
            return await _dbSet.AnyAsync(e => e.Id == id);
        }

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? filter = null)
        {
            var query = _dbSet.AsQueryable();

            if (filter != null)
                query = query.Where(filter);

            return await query.CountAsync();
        }
    }
}
