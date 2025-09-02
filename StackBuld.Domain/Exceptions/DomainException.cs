using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackBuld.Domain.Exceptions
{
    public class DomainException : Exception
    {
        public DomainException(string message) : base(message) { }
        public DomainException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class InsufficientStockException : DomainException
    {
        public InsufficientStockException(string message) : base(message) { }
    }

    public class ProductNotFoundException : DomainException
    {
        public ProductNotFoundException(Guid productId)
            : base($"Product with ID {productId} was not found") { }
    }

    public class OrderNotFoundException : DomainException
    {
        public OrderNotFoundException(Guid orderId)
            : base($"Order with ID {orderId} was not found") { }
    }

    public class ConcurrencyException : DomainException
    {
        public ConcurrencyException(string message) : base(message) { }
    }
}
