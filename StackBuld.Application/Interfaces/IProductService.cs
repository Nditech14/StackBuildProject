using StackBuld.Application.AuthResponse;
using StackBuld.Core.DTOs.ProductDtos;

namespace StackBuld.Application.Interfaces
{
    public interface IProductService
    {
        Task<ServiceResponse<ProductDto>> CreateAsync(CreateProductDto createProductDto);
        Task<ServiceResponse<bool>> DeleteAsync(Guid id);
        Task<ServiceResponse<PaginatedResponse<ProductDto>>> GetAllAsync(int page = 1, int pageSize = 10, bool includeInactive = false);
        Task<ServiceResponse<ProductDto>> GetByIdAsync(Guid id);
        Task<ServiceResponse<List<ProductDto>>> GetByIdsAsync(List<Guid> ids);
        Task<ServiceResponse<ProductDto>> UpdateAsync(Guid id, UpdateProductDto updateProductDto);
        Task<ServiceResponse<ProductDto>> UpdateStockAsync(Guid id, ProductStockUpdateDto stockUpdateDto);
    }
}