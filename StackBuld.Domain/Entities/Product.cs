using StackBuld.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackBuld.Domain.Entities
{
    public class Product : BaseEntity
    {
        private Product() { } // EF Core constructor

        public Product(string name, string description, decimal price, int stockQuantity)
        {
            SetName(name);
            SetDescription(description);
            SetPrice(price);
            SetStockQuantity(stockQuantity);
        }

        [Required]
        [StringLength(200)]
        public string Name { get; private set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; private set; } = string.Empty;

        [Range(0.01, double.MaxValue)]
        public decimal Price { get; private set; }

        [Range(0, int.MaxValue)]
        public int StockQuantity { get; private set; }

        public bool IsActive { get; private set; } = true;

        // Navigation properties
        public virtual ICollection<OrderItem> OrderItems { get; private set; } = new List<OrderItem>();

        // Domain methods
        public void UpdateDetails(string name, string description, decimal price)
        {
            SetName(name);
            SetDescription(description);
            SetPrice(price);
            UpdateTimestamp();
        }

        public void UpdateStock(int newQuantity)
        {
            SetStockQuantity(newQuantity);
            UpdateTimestamp();
        }

        public void ReserveStock(int quantity)
        {
            if (quantity <= 0)
                throw new DomainException("Quantity must be positive");

            if (StockQuantity < quantity)
                throw new InsufficientStockException($"Not enough stock for product {Name}. Available: {StockQuantity}, Required: {quantity}");

            StockQuantity -= quantity;
            UpdateTimestamp();
        }

        public void RestoreStock(int quantity)
        {
            if (quantity <= 0)
                throw new DomainException("Quantity must be positive");

            StockQuantity += quantity;
            UpdateTimestamp();
        }

        public void Activate()
        {
            IsActive = true;
            UpdateTimestamp();
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdateTimestamp();
        }

        private void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Product name cannot be empty");
            if (name.Length > 200)
                throw new DomainException("Product name cannot exceed 200 characters");
            Name = name.Trim();
        }

        private void SetDescription(string description)
        {
            Description = description?.Trim() ?? string.Empty;
            if (Description.Length > 1000)
                throw new DomainException("Product description cannot exceed 1000 characters");
        }

        private void SetPrice(decimal price)
        {
            if (price <= 0)
                throw new DomainException("Product price must be greater than zero");
            Price = price;
        }

        private void SetStockQuantity(int quantity)
        {
            if (quantity < 0)
                throw new DomainException("Stock quantity cannot be negative");
            StockQuantity = quantity;
        }
    }
}
