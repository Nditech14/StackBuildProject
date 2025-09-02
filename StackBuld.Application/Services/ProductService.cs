using AutoMapper;
using Microsoft.Extensions.Logging;
using StackBuld.Application.AuthResponse;
using StackBuld.Application.Interfaces;
using StackBuld.Core.DTOs.ProductDtos;
using StackBuld.Domain.Entities;
using StackBuld.Domain.Exceptions;
using StackBuld.Infrastructure.Services.Abstraction;

namespace StackBuld.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductService> _logger;

        public ProductService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ProductService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ServiceResponse<ProductDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(id);

                if (product == null)
                {
                    return ServiceResponse<ProductDto>.Failure("Product not found", 404);
                }

                var productDto = _mapper.Map<ProductDto>(product);
                return ServiceResponse<ProductDto>.Successful(productDto, "Product retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product {ProductId}", id);
                return ServiceResponse<ProductDto>.Failure("An error occurred while retrieving the product", 500);
            }
        }

        public async Task<ServiceResponse<PaginatedResponse<ProductDto>>> GetAllAsync(int page = 1, int pageSize = 10, bool includeInactive = false)
        {
            try
            {
                var (products, totalCount) = await _unitOfWork.Products.GetPagedAsync(
                    page,
                    pageSize,
                    filter: includeInactive ? null : p => p.IsActive,
                    orderBy: query => query.OrderBy(p => p.Name));

                var productDtos = _mapper.Map<List<ProductDto>>(products);

                var result = new PaginatedResponse<ProductDto>
                {
                    Data = productDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };

                return ServiceResponse<PaginatedResponse<ProductDto>>.Successful(result, "Products retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products page {Page}", page);
                return ServiceResponse<PaginatedResponse<ProductDto>>.Failure("An error occurred while retrieving products", 500);
            }
        }

        public async Task<ServiceResponse<ProductDto>> CreateAsync(CreateProductDto createProductDto)
        {
            try
            {
                var product = new Product(
                    createProductDto.Name,
                    createProductDto.Description,
                    createProductDto.Price,
                    createProductDto.StockQuantity);

                await _unitOfWork.Products.AddAsync(product);
                await _unitOfWork.SaveChangesAsync();

                var productDto = _mapper.Map<ProductDto>(product);
                _logger.LogInformation("Created product {ProductId}: {ProductName}", product.Id, product.Name);

                return ServiceResponse<ProductDto>.Successful(productDto, "Product created successfully", 201);
            }
            catch (DomainException ex)
            {
                return ServiceResponse<ProductDto>.Failure(ex.Message, 400);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product {ProductName}", createProductDto.Name);
                return ServiceResponse<ProductDto>.Failure("An error occurred while creating the product", 500);
            }
        }

        public async Task<ServiceResponse<ProductDto>> UpdateAsync(Guid id, UpdateProductDto updateProductDto)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(id, trackChanges: true);

                if (product == null)
                {
                    return ServiceResponse<ProductDto>.Failure("Product not found", 404);
                }

                if (!string.IsNullOrWhiteSpace(updateProductDto.Name) ||
                    !string.IsNullOrWhiteSpace(updateProductDto.Description) ||
                    updateProductDto.Price.HasValue)
                {
                    product.UpdateDetails(
                        updateProductDto.Name ?? product.Name,
                        updateProductDto.Description ?? product.Description,
                        updateProductDto.Price ?? product.Price);
                }

                if (updateProductDto.StockQuantity.HasValue)
                {
                    product.UpdateStock(updateProductDto.StockQuantity.Value);
                }

                if (updateProductDto.IsActive.HasValue)
                {
                    if (updateProductDto.IsActive.Value)
                        product.Activate();
                    else
                        product.Deactivate();
                }

                await _unitOfWork.SaveChangesAsync();

                var productDto = _mapper.Map<ProductDto>(product);
                _logger.LogInformation("Updated product {ProductId}: {ProductName}", product.Id, product.Name);

                return ServiceResponse<ProductDto>.Successful(productDto, "Product updated successfully");
            }
            catch (ConcurrencyException ex)
            {
                return ServiceResponse<ProductDto>.Failure(ex.Message, 409);
            }
            catch (DomainException ex)
            {
                return ServiceResponse<ProductDto>.Failure(ex.Message, 400);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", id);
                return ServiceResponse<ProductDto>.Failure("An error occurred while updating the product", 500);
            }
        }

        public async Task<ServiceResponse<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(id, trackChanges: true);

                if (product == null)
                {
                    return ServiceResponse<bool>.Failure("Product not found", 404);
                }

                _unitOfWork.Products.Remove(product);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Deleted product {ProductId}: {ProductName}", product.Id, product.Name);
                return ServiceResponse<bool>.Successful(true, "Product deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", id);
                return ServiceResponse<bool>.Failure("An error occurred while deleting the product", 500);
            }
        }

        public async Task<ServiceResponse<ProductDto>> UpdateStockAsync(Guid id, ProductStockUpdateDto stockUpdateDto)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(id, trackChanges: true);

                if (product == null)
                {
                    return ServiceResponse<ProductDto>.Failure("Product not found", 404);
                }

                product.UpdateStock(stockUpdateDto.StockQuantity);
                await _unitOfWork.SaveChangesAsync();

                var productDto = _mapper.Map<ProductDto>(product);
                _logger.LogInformation("Updated stock for product {ProductId}: {StockQuantity}", product.Id, product.StockQuantity);

                return ServiceResponse<ProductDto>.Successful(productDto, "Stock updated successfully");
            }
            catch (ConcurrencyException ex)
            {
                return ServiceResponse<ProductDto>.Failure(ex.Message, 409);
            }
            catch (DomainException ex)
            {
                return ServiceResponse<ProductDto>.Failure(ex.Message, 400);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock for product {ProductId}", id);
                return ServiceResponse<ProductDto>.Failure("An error occurred while updating the stock", 500);
            }
        }

        public async Task<ServiceResponse<List<ProductDto>>> GetByIdsAsync(List<Guid> ids)
        {
            try
            {
                if (ids == null || !ids.Any())
                {
                    return ServiceResponse<List<ProductDto>>.Failure("Product IDs list cannot be empty", 400);
                }

                if (ids.Count > 100)
                {
                    return ServiceResponse<List<ProductDto>>.Failure("Cannot retrieve more than 100 products at once", 400);
                }

                var products = await _unitOfWork.Products.GetByIdsAsync(ids, trackChanges: false);
                var productDtos = _mapper.Map<List<ProductDto>>(products);

                return ServiceResponse<List<ProductDto>>.Successful(productDtos, "Products retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products by IDs");
                return ServiceResponse<List<ProductDto>>.Failure("An error occurred while retrieving products", 500);
            }
        }
    }
}

