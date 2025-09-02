using StackBuld.Domain.Entities;
using StackBuld.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace StackBuld.Infrastructure.Repositories.OrderRepo.Abstraction
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<Order?> GetByOrderNumberAsync(string orderNumber, bool trackChanges = false);
        Task<List<Order>> GetByCustomerEmailAsync(string customerEmail, bool trackChanges = false);
        Task<(List<Order> Orders, int TotalCount)> GetByCustomerEmailPagedAsync(
            string customerEmail, int page, int pageSize, bool trackChanges = false);
        Task<(List<Order> Items, int TotalCount)> GetPagedAsync(
    int page,
    int pageSize,
    Expression<Func<Order, bool>>? filter = null,
    Func<IQueryable<Order>, IOrderedQueryable<Order>>? orderBy = null,
    bool trackChanges = false);
        Task<List<Order>> GetByStatusAsync(OrderStatus status, bool trackChanges = false);
        Task<Order?> GetWithItemsAsync(Guid orderId, bool trackChanges = true);
    }
}
