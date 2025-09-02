using StackBuld.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackBuld.Infrastructure.Repositories.OrderRepo.Abstraction
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<List<Product>> GetActiveProductsAsync(bool trackChanges = false);
        Task<List<Product>> GetByIdsAsync(List<Guid> ids, bool trackChanges = true);
        Task<bool> ReserveStockAsync(Guid productId, int quantity);
        Task<bool> RestoreStockAsync(Guid productId, int quantity);
        Task<Dictionary<Guid, int>> GetStockLevelsAsync(List<Guid> productIds);
    }
}
