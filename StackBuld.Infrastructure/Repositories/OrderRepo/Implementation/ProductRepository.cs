using Microsoft.EntityFrameworkCore;
using StackBuld.Domain.Entities;
using StackBuld.Domain.Exceptions;
using StackBuld.Infrastructure.Data;
using StackBuld.Infrastructure.Repositories.OrderRepo.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackBuld.Infrastructure.Repositories.OrderRepo.Implementation
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        public ProductRepository(ECommerceDbContext context) : base(context)
        {
        }

        public async Task<List<Product>> GetActiveProductsAsync(bool trackChanges = false)
        {
            var query = _dbSet.AsQueryable();

            if (!trackChanges)
                query = query.AsNoTracking();

            return await query.Where(p => p.IsActive).ToListAsync();
        }

        public async Task<List<Product>> GetByIdsAsync(List<Guid> ids, bool trackChanges = true)
        {
            var query = _dbSet.AsQueryable();

            if (!trackChanges)
                query = query.AsNoTracking();

            return await query.Where(p => ids.Contains(p.Id)).ToListAsync();
        }

        public async Task<bool> ReserveStockAsync(Guid productId, int quantity)
        {
            // This method uses optimistic concurrency control with row versioning
            var product = await _dbSet.FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                throw new ProductNotFoundException(productId);

            if (!product.IsActive)
                throw new DomainException($"Product {product.Name} is not active");

            try
            {
                product.ReserveStock(quantity);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Reload and try once more
                await _context.Entry(product).ReloadAsync();

                if (product.StockQuantity < quantity)
                    throw new InsufficientStockException($"Not enough stock for product {product.Name}. Available: {product.StockQuantity}, Required: {quantity}");

                product.ReserveStock(quantity);
                await _context.SaveChangesAsync();
                return true;
            }
        }

        public async Task<bool> RestoreStockAsync(Guid productId, int quantity)
        {
            var product = await _dbSet.FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                throw new ProductNotFoundException(productId);

            try
            {
                product.RestoreStock(quantity);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                await _context.Entry(product).ReloadAsync();
                product.RestoreStock(quantity);
                await _context.SaveChangesAsync();
                return true;
            }
        }

        public async Task<Dictionary<Guid, int>> GetStockLevelsAsync(List<Guid> productIds)
        {
            return await _dbSet
                .Where(p => productIds.Contains(p.Id))
                .Select(p => new { p.Id, p.StockQuantity })
                .ToDictionaryAsync(x => x.Id, x => x.StockQuantity);
        }
    }
}

