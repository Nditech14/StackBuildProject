using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StackBuld.Application.Interfaces;
using StackBuld.Core.DTOs.ProductDtos;

namespace Web.Api.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<ActionResult> GetProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool includeInactive = false)
        {
            var result = await _productService.GetAllAsync(page, pageSize, includeInactive);

            if (!result.Success)
                return StatusCode(result.StatusCode, new { message = result.Message });

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetProduct(Guid id)
        {
            var result = await _productService.GetByIdAsync(id);

            if (!result.Success)
                return StatusCode(result.StatusCode, new { message = result.Message });

            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult> CreateProduct(
            [FromBody] CreateProductDto createProductDto,
            [FromServices] IValidator<CreateProductDto> validator)
        {
            var validationResult = await validator.ValidateAsync(createProductDto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(x => x.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

                return BadRequest(new
                {
                    success = false,
                    message = "Validation failed",
                    errors = errors,
                    statusCode = 400
                });
            }

            var result = await _productService.CreateAsync(createProductDto);

            if (!result.Success)
                return StatusCode(result.StatusCode, new { message = result.Message });

            return CreatedAtAction(nameof(GetProduct), new { id = result.Data?.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateProduct(
            Guid id,
            [FromBody] UpdateProductDto updateProductDto,
            [FromServices] IValidator<UpdateProductDto> validator)
        {
            var validationResult = await validator.ValidateAsync(updateProductDto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(x => x.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

                return BadRequest(new
                {
                    success = false,
                    message = "Validation failed",
                    errors = errors,
                    statusCode = 400
                });
            }

            var result = await _productService.UpdateAsync(id, updateProductDto);

            if (!result.Success)
                return StatusCode(result.StatusCode, new { message = result.Message });

            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteProduct(Guid id)
        {
            var result = await _productService.DeleteAsync(id);

            if (!result.Success)
                return StatusCode(result.StatusCode, new { message = result.Message });

            return Ok(result);
        }

        [HttpPatch("{id}/stock")]
        public async Task<ActionResult> UpdateStock(
            Guid id,
            [FromBody] ProductStockUpdateDto stockUpdateDto,
            [FromServices] IValidator<ProductStockUpdateDto> validator)
        {
            var validationResult = await validator.ValidateAsync(stockUpdateDto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(x => x.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

                return BadRequest(new
                {
                    success = false,
                    message = "Validation failed",
                    errors = errors,
                    statusCode = 400
                });
            }

            var result = await _productService.UpdateStockAsync(id, stockUpdateDto);

            if (!result.Success)
                return StatusCode(result.StatusCode, new { message = result.Message });

            return Ok(result);
        }
    }


}
