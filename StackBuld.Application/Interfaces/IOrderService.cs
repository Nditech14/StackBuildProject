using StackBuld.Application.AuthResponse;
using StackBuld.Core.DTOs.OrderDtos;

namespace StackBuld.Application.Interfaces
{
    public interface IOrderService
    {
        Task<ServiceResponse<OrderDto>> AddItemAsync(Guid orderId, CreateOrderItemDto itemDto);
        Task<ServiceResponse<OrderDto>> CancelAsync(Guid orderId);
        Task<ServiceResponse<OrderDto>> ConfirmAsync(Guid orderId);
        Task<ServiceResponse<OrderDto>> CreateAsync(CreateOrderDto createOrderDto);
        Task<ServiceResponse<OrderDto>> DeliverAsync(Guid orderId);
        Task<ServiceResponse<PaginatedResponse<OrderDto>>> GetAllAsync(int page = 1, int pageSize = 10);
        Task<ServiceResponse<PaginatedResponse<OrderDto>>> GetByCustomerEmailAsync(string customerEmail, int page = 1, int pageSize = 10);
        Task<ServiceResponse<OrderDto>> GetByIdAsync(Guid id);
        Task<ServiceResponse<OrderDto>> RemoveItemAsync(Guid orderId, Guid productId);
        Task<ServiceResponse<OrderDto>> ShipAsync(Guid orderId);
        Task<ServiceResponse<OrderDto>> UpdateItemAsync(Guid orderId, UpdateOrderItemDto itemDto);
    }
}