using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StackBuld.Application.Interfaces;
using StackBuld.Core.DTOs.OrderDtos;

namespace Web.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost]
        public async Task<ActionResult> CreateOrder(
            [FromBody] CreateOrderDto createOrderDto,
            [FromServices] IValidator<CreateOrderDto> validator)
        {
            var validationResult = await validator.ValidateAsync(createOrderDto);
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

            var result = await _orderService.CreateAsync(createOrderDto);

            if (!result.Success)
                return StatusCode(result.StatusCode, new { message = result.Message });

            return CreatedAtAction(nameof(GetOrder), new { id = result.Data?.Id }, result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetOrder(Guid id)
        {
            var result = await _orderService.GetByIdAsync(id);

            if (!result.Success)
                return StatusCode(result.StatusCode, new { message = result.Message });

            return Ok(result);
        }

        [HttpGet]
        public async Task<ActionResult> GetOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _orderService.GetAllAsync(page, pageSize);

            if (!result.Success)
                return StatusCode(result.StatusCode, new { message = result.Message });

            return Ok(result);
        }

        [HttpGet("customer/{customerEmail}")]
        public async Task<ActionResult> GetOrdersByCustomer(
            string customerEmail,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _orderService.GetByCustomerEmailAsync(customerEmail, page, pageSize);

            if (!result.Success)
                return StatusCode(result.StatusCode, new { message = result.Message });

            return Ok(result);
        }

        [HttpPatch("{id}/confirm")]
        public async Task<ActionResult> ConfirmOrder(Guid id)
        {
            var result = await _orderService.ConfirmAsync(id);

            if (!result.Success)
                return StatusCode(result.StatusCode, new { message = result.Message });

            return Ok(result);
        }

        [HttpPatch("{id}/cancel")]
        public async Task<ActionResult> CancelOrder(Guid id)
        {
            var result = await _orderService.CancelAsync(id);

            if (!result.Success)
                return StatusCode(result.StatusCode, new { message = result.Message });

            return Ok(result);
        }

        [HttpPatch("{id}/ship")]
        public async Task<ActionResult> ShipOrder(Guid id)
        {
            var result = await _orderService.ShipAsync(id);

            if (!result.Success)
                return StatusCode(result.StatusCode, new { message = result.Message });

            return Ok(result);
        }

        [HttpPatch("{id}/deliver")]
        public async Task<ActionResult> DeliverOrder(Guid id)
        {
            var result = await _orderService.DeliverAsync(id);

            if (!result.Success)
                return StatusCode(result.StatusCode, new { message = result.Message });

            return Ok(result);
        }

        [HttpPost("{id}/items")]
        public async Task<ActionResult> AddOrderItem(
            Guid id,
            [FromBody] CreateOrderItemDto itemDto,
            [FromServices] IValidator<CreateOrderItemDto> validator)
        {
            var validationResult = await validator.ValidateAsync(itemDto);
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

            var result = await _orderService.AddItemAsync(id, itemDto);

            if (!result.Success)
                return StatusCode(result.StatusCode, new { message = result.Message });

            return Ok(result);
        }

        [HttpPut("{id}/items")]
        public async Task<ActionResult> UpdateOrderItem(
            Guid id,
            [FromBody] UpdateOrderItemDto itemDto,
            [FromServices] IValidator<UpdateOrderItemDto> validator)
        {
            var validationResult = await validator.ValidateAsync(itemDto);
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

            var result = await _orderService.UpdateItemAsync(id, itemDto);

            if (!result.Success)
                return StatusCode(result.StatusCode, new { message = result.Message });

            return Ok(result);
        }

        [HttpDelete("{id}/items/{productId}")]
        public async Task<ActionResult> RemoveOrderItem(Guid id, Guid productId)
        {
            var result = await _orderService.RemoveItemAsync(id, productId);

            if (!result.Success)
                return StatusCode(result.StatusCode, new { message = result.Message });

            return Ok(result);
        }
    }
}