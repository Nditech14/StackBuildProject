using StackBuld.Domain.Enums;
using StackBuld.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace StackBuld.Domain.Entities
{
    public class Order : BaseEntity
    {
        private readonly List<OrderItem> _orderItems = new();

        private Order() { } // EF Core constructor

        public Order(string customerEmail)
        {
            SetCustomerEmail(customerEmail);
            OrderNumber = GenerateOrderNumber();
            Status = OrderStatus.Pending;
        }

        [Required]
        [StringLength(50)]
        public string OrderNumber { get; private set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string CustomerEmail { get; private set; } = string.Empty;
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public OrderStatus Status { get; private set; }

        public decimal TotalAmount { get; private set; }

        public DateTime? ProcessedAt { get; private set; }

        // Navigation properties
        public virtual IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();

        // Domain methods
        public void AddItem(Product product, int quantity)
        {
            if (Status != OrderStatus.Pending)
                throw new DomainException("Cannot modify confirmed order");

            if (product == null)
                throw new DomainException("Product cannot be null");

            if (quantity <= 0)
                throw new DomainException("Quantity must be positive");

            var existingItem = _orderItems.FirstOrDefault(x => x.ProductId == product.Id);
            if (existingItem != null)
            {
                existingItem.UpdateQuantity(existingItem.Quantity + quantity);
            }
            else
            {
                var orderItem = new OrderItem(product, quantity);
                _orderItems.Add(orderItem);
            }

            RecalculateTotal();
            UpdateTimestamp();
        }

        public void RemoveItem(Guid productId)
        {
            if (Status != OrderStatus.Pending)
                throw new DomainException("Cannot modify confirmed order");

            var item = _orderItems.FirstOrDefault(x => x.ProductId == productId);
            if (item != null)
            {
                _orderItems.Remove(item);
                RecalculateTotal();
                UpdateTimestamp();
            }
        }

        public void UpdateItemQuantity(Guid productId, int newQuantity)
        {
            if (Status != OrderStatus.Pending)
                throw new DomainException("Cannot modify confirmed order");

            if (newQuantity <= 0)
            {
                RemoveItem(productId);
                return;
            }

            var item = _orderItems.FirstOrDefault(x => x.ProductId == productId);
            if (item != null)
            {
                item.UpdateQuantity(newQuantity);
                RecalculateTotal();
                UpdateTimestamp();
            }
        }

        public void Confirm()
        {
            if (Status != OrderStatus.Pending)
                throw new DomainException("Order is not in pending state");

            if (!_orderItems.Any())
                throw new DomainException("Cannot confirm order without items");

            Status = OrderStatus.Confirmed;
            ProcessedAt = DateTime.UtcNow;
            UpdateTimestamp();
        }

        public void Cancel()
        {
            if (Status == OrderStatus.Cancelled)
                return;

            if (Status != OrderStatus.Pending && Status != OrderStatus.Confirmed)
                throw new DomainException($"Cannot cancel order with status: {Status}");

            Status = OrderStatus.Cancelled;
            UpdateTimestamp();
        }

        public void Ship()
        {
            if (Status != OrderStatus.Confirmed)
                throw new DomainException("Only confirmed orders can be shipped");

            Status = OrderStatus.Shipped;
            UpdateTimestamp();
        }

        public void Deliver()
        {
            if (Status != OrderStatus.Shipped)
                throw new DomainException("Only shipped orders can be delivered");

            Status = OrderStatus.Delivered;
            UpdateTimestamp();
        }

        private void SetCustomerEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new DomainException("Customer email cannot be empty");
            if (!IsValidEmail(email))
                throw new DomainException("Invalid email format");
            CustomerEmail = email.Trim().ToLowerInvariant();
        }

        private void RecalculateTotal()
        {
            TotalAmount = _orderItems.Sum(item => item.TotalPrice);
        }

        private static string GenerateOrderNumber()
        {
            return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
