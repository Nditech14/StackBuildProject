using Microsoft.EntityFrameworkCore;
using StackBuld.Domain.Entities;
using StackBuld.Domain.Enums;
using StackBuld.Infrastructure.Data;
using StackBuld.Infrastructure.Repositories.OrderRepo.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace StackBuld.Infrastructure.Repositories.OrderRepo.Implementation
{
    public class OrderRepository : Repository<Order>, IOrderRepository
    {
        public OrderRepository(ECommerceDbContext context) : base(context)
        {
        }

        public async Task<Order?> GetByOrderNumberAsync(string orderNumber, bool trackChanges = false)
        {
            var query = _dbSet.Include(o => o.OrderItems).AsQueryable();

            if (!trackChanges)
                query = query.AsNoTracking();

            return await query.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
        }

        public async Task<List<Order>> GetByCustomerEmailAsync(string customerEmail, bool trackChanges = false)
        {
            var query = _dbSet.Include(o => o.OrderItems).AsQueryable();

            if (!trackChanges)
                query = query.AsNoTracking();

            return await query
                .Where(o => o.CustomerEmail == customerEmail.ToLowerInvariant())
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<(List<Order> Orders, int TotalCount)> GetByCustomerEmailPagedAsync(
            string customerEmail, int page, int pageSize, bool trackChanges = false)
        {
            var query = _dbSet.Include(o => o.OrderItems).AsQueryable();

            if (!trackChanges)
                query = query.AsNoTracking();

            query = query.Where(o => o.CustomerEmail == customerEmail.ToLowerInvariant());

            var totalCount = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (orders, totalCount);
        }

        public async Task<List<Order>> GetByStatusAsync(OrderStatus status, bool trackChanges = false)
        {
            var query = _dbSet.Include(o => o.OrderItems).AsQueryable();

            if (!trackChanges)
                query = query.AsNoTracking();

            return await query.Where(o => o.Status == status).ToListAsync();
        }

        public async Task<Order?> GetWithItemsAsync(Guid orderId, bool trackChanges = true)
        {
            var query = _dbSet
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .AsQueryable();

            if (!trackChanges)
                query = query.AsNoTracking();

            return await query.FirstOrDefaultAsync(o => o.Id == orderId);
        }
        public override async Task<(List<Order> Items, int TotalCount)> GetPagedAsync(
    int page,
    int pageSize,
    Expression<Func<Order, bool>>? filter = null,
    Func<IQueryable<Order>, IOrderedQueryable<Order>>? orderBy = null,
    bool trackChanges = false)
        {
            var query = _dbSet.Include(o => o.OrderItems).AsQueryable(); // Include OrderItems

            if (!trackChanges)
                query = query.AsNoTracking();

            if (filter != null)
                query = query.Where(filter);

            var totalCount = await query.CountAsync();

            if (orderBy != null)
                query = orderBy(query);
            else
                query = query.OrderByDescending(o => o.CreatedAt); // Default ordering for orders

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
        public override async Task<Order?> GetByIdAsync(Guid id, bool trackChanges = false)
        {
            var query = _dbSet.Include(o => o.OrderItems).AsQueryable();

            if (!trackChanges)
                query = query.AsNoTracking();

            return await query.FirstOrDefaultAsync(o => o.Id == id);
        }
    }
}
