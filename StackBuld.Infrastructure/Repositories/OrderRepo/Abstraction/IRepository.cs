using StackBuld.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace StackBuld.Infrastructure.Repositories.OrderRepo.Abstraction
{
    public interface IRepository<T> where T : BaseEntity
    {
        Task<T?> GetByIdAsync(Guid id, bool trackChanges = false);
        Task<List<T>> GetAllAsync(bool trackChanges = false);
        Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, bool trackChanges = false);
        Task<T?> FindFirstAsync(Expression<Func<T, bool>> predicate, bool trackChanges = false);
        Task<(List<T> Items, int TotalCount)> GetPagedAsync(
            int page,
            int pageSize,
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            bool trackChanges = false);
        Task<T> AddAsync(T entity);
        Task<List<T>> AddRangeAsync(List<T> entities);
        void Update(T entity);
        void UpdateRange(List<T> entities);
        void Remove(T entity);
        void RemoveRange(List<T> entities);
        Task<bool> ExistsAsync(Guid id);
        Task<int> CountAsync(Expression<Func<T, bool>>? filter = null);
    }
}
