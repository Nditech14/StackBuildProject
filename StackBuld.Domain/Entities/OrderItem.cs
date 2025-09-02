using StackBuld.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackBuld.Domain.Entities
{
    public class OrderItem : BaseEntity
    {
        private OrderItem() { } // EF Core constructor

        public OrderItem(Product product, int quantity)
        {
            if (product == null)
                throw new DomainException("Product cannot be null");

            ProductId = product.Id;
            ProductName = product.Name; // Snapshot for historical reference
            UnitPrice = product.Price;   // Snapshot for historical reference
            UpdateQuantity(quantity);
        }

        public Guid OrderId { get; private set; }

        public Guid ProductId { get; private set; }

        [Required]
        [StringLength(200)]
        public string ProductName { get; private set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int Quantity { get; private set; }

        [Range(0.01, double.MaxValue)]
        public decimal UnitPrice { get; private set; }

        public decimal TotalPrice { get; private set; }

        // Navigation properties
        public virtual Order Order { get; private set; } = null!;
        public virtual Product Product { get; private set; } = null!;

        // Domain methods
        public void UpdateQuantity(int newQuantity)
        {
            if (newQuantity <= 0)
                throw new DomainException("Quantity must be positive");

            Quantity = newQuantity;
            TotalPrice = UnitPrice * Quantity;
            UpdateTimestamp();
        }
    }
}
