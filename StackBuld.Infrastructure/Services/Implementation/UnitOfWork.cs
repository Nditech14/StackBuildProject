using Microsoft.EntityFrameworkCore.Storage;
using StackBuld.Infrastructure.Data;
using StackBuld.Infrastructure.Repositories.OrderRepo.Abstraction;
using StackBuld.Infrastructure.Repositories.OrderRepo.Implementation;
using StackBuld.Infrastructure.Services.Abstraction;

namespace StackBuld.Infrastructure.Services.Implementation
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ECommerceDbContext _context;
        private IProductRepository? _products;
        private IOrderRepository? _orders;
        private bool _disposed = false;

        public UnitOfWork(ECommerceDbContext context)
        {
            _context = context;
        }

        public IProductRepository Products => _products ??= new ProductRepository(_context);
        public IOrderRepository Orders => _orders ??= new OrderRepository(_context);

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task BeginTransactionAsync()
        {
            await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_context.Database.CurrentTransaction != null)
            {
                await _context.Database.CommitTransactionAsync();
            }
        }
        public IExecutionStrategy GetExecutionStrategy()
        {
            return _context.Database.CreateExecutionStrategy();
        }

        public async Task RollbackTransactionAsync()
        {
            if (_context.Database.CurrentTransaction != null)
            {
                await _context.Database.RollbackTransactionAsync();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _context.Dispose();
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
