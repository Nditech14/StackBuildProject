using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackBuld.Application.AuthResponse;
using StackBuld.Application.Interfaces;
using StackBuld.Core.DTOs.OrderDtos;
using StackBuld.Domain.Entities;
using StackBuld.Domain.Exceptions;
using StackBuld.Infrastructure.Services.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackBuld.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<OrderService> _logger;

        public OrderService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<OrderService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ServiceResponse<OrderDto>> CreateAsync(CreateOrderDto createOrderDto)
        {
            var strategy = _unitOfWork.GetExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await _unitOfWork.BeginTransactionAsync();

                try
                {
             
                    var order = new Order(createOrderDto.CustomerEmail);

                
                    var productIds = createOrderDto.Items.Select(x => x.ProductId).ToList();
                    var products = await _unitOfWork.Products.GetByIdsAsync(productIds, trackChanges: true);

                   
                    var missingProducts = productIds.Except(products.Select(p => p.Id)).ToList();
                    if (missingProducts.Any())
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return ServiceResponse<OrderDto>.Failure($"Products not found: {string.Join(", ", missingProducts)}", 404);
                    }

                    var inactiveProducts = products.Where(p => !p.IsActive).ToList();
                    if (inactiveProducts.Any())
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return ServiceResponse<OrderDto>.Failure($"Inactive products: {string.Join(", ", inactiveProducts.Select(p => p.Name))}", 400);
                    }

                    // Check stock availability and reserve stock
                    foreach (var item in createOrderDto.Items)
                    {
                        var product = products.First(p => p.Id == item.ProductId);

                        if (product.StockQuantity < item.Quantity)
                        {
                            await _unitOfWork.RollbackTransactionAsync();
                            return ServiceResponse<OrderDto>.Failure($"Insufficient stock for {product.Name}. Available: {product.StockQuantity}, Required: {item.Quantity}", 409);
                        }

                        // Reserve the stock (this will throw InsufficientStockException if concurrent modification)
                        try
                        {
                            product.ReserveStock(item.Quantity);
                        }
                        catch (InsufficientStockException ex)
                        {
                            await _unitOfWork.RollbackTransactionAsync();
                            return ServiceResponse<OrderDto>.Failure(ex.Message, 409);
                        }

                        // Add item to order
                        order.AddItem(product, item.Quantity);
                    }

                    // Save order
                    await _unitOfWork.Orders.AddAsync(order);
                    await _unitOfWork.SaveChangesAsync();

                    await _unitOfWork.CommitTransactionAsync();

                    // Return the created order
                    var savedOrder = await _unitOfWork.Orders.GetWithItemsAsync(order.Id, trackChanges: false);
                    var orderDto = _mapper.Map<OrderDto>(savedOrder);

                    _logger.LogInformation("Created order {OrderNumber} for {CustomerEmail} with {ItemCount} items",
                        order.OrderNumber, order.CustomerEmail, order.OrderItems.Count);

                    return ServiceResponse<OrderDto>.Successful(orderDto, "Order created successfully", 201);
                }
                catch (ConcurrencyException ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<OrderDto>.Failure("Stock levels were modified by another process. Please try again.", 409);
                }
                catch (DomainException ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<OrderDto>.Failure(ex.Message, 400);
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogError(ex, "Error creating order for {CustomerEmail}", createOrderDto.CustomerEmail);
                    return ServiceResponse<OrderDto>.Failure("An error occurred while creating the order", 500);
                }
            }); 
        }

        public async Task<ServiceResponse<OrderDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var order = await _unitOfWork.Orders.GetByIdAsync(id);

                if (order == null)
                {
                    return ServiceResponse<OrderDto>.Failure("Order not found", 404);
                }

                var orderDto = _mapper.Map<OrderDto>(order);
                return ServiceResponse<OrderDto>.Successful(orderDto, "Order retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order {OrderId}", id);
                return ServiceResponse<OrderDto>.Failure("An error occurred while retrieving the order", 500);
            }
        }

        public async Task<ServiceResponse<PaginatedResponse<OrderDto>>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            try
            {
                var (orders, totalCount) = await _unitOfWork.Orders.GetPagedAsync(
                    page,
                    pageSize,
                    orderBy: query => query.OrderByDescending(o => o.CreatedAt));

                var orderDtos = _mapper.Map<List<OrderDto>>(orders);

                var result = new PaginatedResponse<OrderDto>
                {
                    Data = orderDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };

                return ServiceResponse<PaginatedResponse<OrderDto>>.Successful(result, "Orders retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders page {Page}", page);
                return ServiceResponse<PaginatedResponse<OrderDto>>.Failure("An error occurred while retrieving orders", 500);
            }
        }

        public async Task<ServiceResponse<PaginatedResponse<OrderDto>>> GetByCustomerEmailAsync(string customerEmail, int page = 1, int pageSize = 10)
        {
            try
            {
                var (orders, totalCount) = await _unitOfWork.Orders.GetByCustomerEmailPagedAsync(
                    customerEmail, page, pageSize);

                var orderDtos = _mapper.Map<List<OrderDto>>(orders);

                var result = new PaginatedResponse<OrderDto>
                {
                    Data = orderDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };

                return ServiceResponse<PaginatedResponse<OrderDto>>.Successful(result, "Customer orders retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders for customer {CustomerEmail} page {Page}", customerEmail, page);
                return ServiceResponse<PaginatedResponse<OrderDto>>.Failure("An error occurred while retrieving customer orders", 500);
            }
        }
        public async Task<ServiceResponse<OrderDto>> ConfirmAsync(Guid orderId)
        {
            try
            {
                var order = await _unitOfWork.Orders.GetByIdAsync(orderId, trackChanges: true);

                if (order == null)
                {
                    return ServiceResponse<OrderDto>.Failure("Order not found", 404);
                }

                order.Confirm();
                await _unitOfWork.SaveChangesAsync();

                var orderDto = _mapper.Map<OrderDto>(order);
                _logger.LogInformation("Confirmed order {OrderNumber}", order.OrderNumber);

                return ServiceResponse<OrderDto>.Successful(orderDto, "Order confirmed successfully");
            }
            catch (DomainException ex)
            {
                return ServiceResponse<OrderDto>.Failure(ex.Message, 400);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming order {OrderId}", orderId);
                return ServiceResponse<OrderDto>.Failure("An error occurred while confirming the order", 500);
            }
        }

        public async Task<ServiceResponse<OrderDto>> CancelAsync(Guid orderId)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var order = await _unitOfWork.Orders.GetWithItemsAsync(orderId, trackChanges: true);

                if (order == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<OrderDto>.Failure("Order not found", 404);
                }

                // Restore stock for all items if order was confirmed
                if (order.Status == Domain.Enums.OrderStatus.Confirmed)
                {
                    foreach (var item in order.OrderItems)
                    {
                        await _unitOfWork.Products.RestoreStockAsync(item.ProductId, item.Quantity);
                    }
                }

                order.Cancel();
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                var orderDto = _mapper.Map<OrderDto>(order);
                _logger.LogInformation("Cancelled order {OrderNumber}", order.OrderNumber);

                return ServiceResponse<OrderDto>.Successful(orderDto, "Order cancelled successfully");
            }
            catch (DomainException ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResponse<OrderDto>.Failure(ex.Message, 400);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
                return ServiceResponse<OrderDto>.Failure("An error occurred while cancelling the order", 500);
            }
        }

        public async Task<ServiceResponse<OrderDto>> AddItemAsync(Guid orderId, CreateOrderItemDto itemDto)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var order = await _unitOfWork.Orders.GetWithItemsAsync(orderId, trackChanges: true);

                if (order == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<OrderDto>.Failure("Order not found", 404);
                }

                // Can only add items to pending orders
                if (order.Status != Domain.Enums.OrderStatus.Pending)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<OrderDto>.Failure("Cannot add items to a non-pending order", 400);
                }

                // Get the product
                var product = await _unitOfWork.Products.GetByIdAsync(itemDto.ProductId, trackChanges: true);

                if (product == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<OrderDto>.Failure("Product not found", 404);
                }

                if (!product.IsActive)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<OrderDto>.Failure($"Product {product.Name} is not active", 400);
                }

                // Check stock availability
                if (product.StockQuantity < itemDto.Quantity)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<OrderDto>.Failure($"Insufficient stock for {product.Name}. Available: {product.StockQuantity}, Required: {itemDto.Quantity}", 409);
                }

                // Reserve stock and add item
                try
                {
                    product.ReserveStock(itemDto.Quantity);
                    order.AddItem(product, itemDto.Quantity);
                }
                catch (InsufficientStockException ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<OrderDto>.Failure(ex.Message, 409);
                }
                catch (DomainException ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<OrderDto>.Failure(ex.Message, 400);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                var orderDto = _mapper.Map<OrderDto>(order);
                _logger.LogInformation("Added item {ProductId} to order {OrderNumber}", itemDto.ProductId, order.OrderNumber);

                return ServiceResponse<OrderDto>.Successful(orderDto, "Item added to order successfully");
            }
            catch (ConcurrencyException ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResponse<OrderDto>.Failure("Stock levels were modified by another process. Please try again.", 409);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error adding item to order {OrderId}", orderId);
                return ServiceResponse<OrderDto>.Failure("An error occurred while adding the item to the order", 500);
            }
        }

        public async Task<ServiceResponse<OrderDto>> UpdateItemAsync(Guid orderId, UpdateOrderItemDto itemDto)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var order = await _unitOfWork.Orders.GetWithItemsAsync(orderId, trackChanges: true);

                if (order == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<OrderDto>.Failure("Order not found", 404);
                }

           
                if (order.Status != Domain.Enums.OrderStatus.Pending)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<OrderDto>.Failure("Cannot update items in a non-pending order", 400);
                }

         
                var orderItem = order.OrderItems.FirstOrDefault(oi => oi.ProductId == itemDto.ProductId);
                if (orderItem == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<OrderDto>.Failure("Order item not found", 404);
                }

              
                var product = await _unitOfWork.Products.GetByIdAsync(itemDto.ProductId, trackChanges: true);

                if (product == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<OrderDto>.Failure("Product not found", 404);
                }

               
                var quantityDifference = itemDto.Quantity - orderItem.Quantity;

                if (quantityDifference > 0)
                {
                    // Need more stock
                    if (product.StockQuantity < quantityDifference)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return ServiceResponse<OrderDto>.Failure($"Insufficient stock for {product.Name}. Available: {product.StockQuantity}, Additional Required: {quantityDifference}", 409);
                    }

                    try
                    {
                        product.ReserveStock(quantityDifference);
                    }
                    catch (InsufficientStockException ex)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return ServiceResponse<OrderDto>.Failure(ex.Message, 409);
                    }
                }
                else if (quantityDifference < 0)
                {
                    // Restore stock
                    product.RestoreStock(Math.Abs(quantityDifference));
                }

                // Update the order item
                try
                {
                    order.UpdateItemQuantity(itemDto.ProductId, itemDto.Quantity);
                }
                catch (DomainException ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<OrderDto>.Failure(ex.Message, 400);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                var orderDto = _mapper.Map<OrderDto>(order);
                _logger.LogInformation("Updated item {ProductId} in order {OrderNumber}", itemDto.ProductId, order.OrderNumber);

                return ServiceResponse<OrderDto>.Successful(orderDto, "Order item updated successfully");
            }
            catch (ConcurrencyException ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResponse<OrderDto>.Failure("Stock levels were modified by another process. Please try again.", 409);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error updating item in order {OrderId}", orderId);
                return ServiceResponse<OrderDto>.Failure("An error occurred while updating the order item", 500);
            }
        }

        public async Task<ServiceResponse<OrderDto>> RemoveItemAsync(Guid orderId, Guid productId)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var order = await _unitOfWork.Orders.GetWithItemsAsync(orderId, trackChanges: true);

                if (order == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<OrderDto>.Failure("Order not found", 404);
                }

               
                if (order.Status != Domain.Enums.OrderStatus.Pending)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<OrderDto>.Failure("Cannot remove items from a non-pending order", 400);
                }

           
                var orderItem = order.OrderItems.FirstOrDefault(oi => oi.ProductId == productId);
                if (orderItem == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<OrderDto>.Failure("Order item not found", 404);
                }

               
                await _unitOfWork.Products.RestoreStockAsync(productId, orderItem.Quantity);

                // Remove the item from the order
                try
                {
                    order.RemoveItem(productId);
                }
                catch (DomainException ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<OrderDto>.Failure(ex.Message, 400);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                var orderDto = _mapper.Map<OrderDto>(order);
                _logger.LogInformation("Removed item {ProductId} from order {OrderNumber}", productId, order.OrderNumber);

                return ServiceResponse<OrderDto>.Successful(orderDto, "Item removed from order successfully");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error removing item from order {OrderId}", orderId);
                return ServiceResponse<OrderDto>.Failure("An error occurred while removing the item from the order", 500);
            }
        }

        public async Task<ServiceResponse<OrderDto>> ShipAsync(Guid orderId)
        {
            try
            {
                var order = await _unitOfWork.Orders.GetByIdAsync(orderId, trackChanges: true);

                if (order == null)
                {
                    return ServiceResponse<OrderDto>.Failure("Order not found", 404);
                }

                order.Ship();
                await _unitOfWork.SaveChangesAsync();

                var orderDto = _mapper.Map<OrderDto>(order);
                _logger.LogInformation("Shipped order {OrderNumber}", order.OrderNumber);

                return ServiceResponse<OrderDto>.Successful(orderDto, "Order shipped successfully");
            }
            catch (DomainException ex)
            {
                return ServiceResponse<OrderDto>.Failure(ex.Message, 400);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error shipping order {OrderId}", orderId);
                return ServiceResponse<OrderDto>.Failure("An error occurred while shipping the order", 500);
            }
        }

        public async Task<ServiceResponse<OrderDto>> DeliverAsync(Guid orderId)
        {
            try
            {
                var order = await _unitOfWork.Orders.GetByIdAsync(orderId, trackChanges: true);

                if (order == null)
                {
                    return ServiceResponse<OrderDto>.Failure("Order not found", 404);
                }

                order.Deliver();
                await _unitOfWork.SaveChangesAsync();

                var orderDto = _mapper.Map<OrderDto>(order);
                _logger.LogInformation("Delivered order {OrderNumber}", order.OrderNumber);

                return ServiceResponse<OrderDto>.Successful(orderDto, "Order delivered successfully");
            }
            catch (DomainException ex)
            {
                return ServiceResponse<OrderDto>.Failure(ex.Message, 400);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error delivering order {OrderId}", orderId);
                return ServiceResponse<OrderDto>.Failure("An error occurred while delivering the order", 500);
            }
        }
    }
}