using BistroGo.Core.Enumerations;
using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BistroGo.Api.Controllers
{
    /// <summary>
    /// RESTful API controller for customer orders and line item management.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderRepository _repository;
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderRepository repository, IOrderService orderService, ILogger<OrdersController>? logger = null)
        {
            _repository = repository;
            _orderService = orderService;
            _logger = logger ?? NullLogger<OrdersController>.Instance;
        }

        /// <summary>Retrieves all orders, optionally filtered by user ID.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> Get([FromQuery] string? userId = null)
        {
            if (!string.IsNullOrEmpty(userId))
            {
                _logger.LogInformation("Retrieving orders for user {UserId}", userId);
                var userOrders = await _repository.GetByUserIdAsync(userId);
                return Ok(userOrders);
            }
            _logger.LogInformation("Retrieving all orders");
            var orders = await _repository.GetAllAsync();
            return Ok(orders);
        }

        /// <summary>Retrieves an order by identifier with status and line items.</summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Order>> GetById(int id)
        {
            _logger.LogInformation("Retrieving order by id {OrderId}", id);
            var order = await _repository.GetByIdAsync(id);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found", id);
                return NotFound();
            }
            return Ok(order);
        }

        /// <summary>Retrieves all orders for a specific user ID.</summary>
        [HttpGet("by-user/{userId}")]
        public async Task<ActionResult<IEnumerable<Order>>> GetByUserId(string userId)
        {
            _logger.LogInformation("Retrieving orders by user id {UserId}", userId);
            var orders = await _repository.GetByUserIdAsync(userId);
            return Ok(orders);
        }

        /// <summary>Creates a new order.</summary>
        [HttpPost]
        public async Task<ActionResult<Order>> Post([FromBody] Order order)
        {
            _logger.LogInformation("Creating new order for user {UserId}", order?.UserId);
            var created = await _repository.AddAsync(order!);
            _logger.LogInformation("Created order {OrderId} successfully", created.Id);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>Updates order details.</summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<Order>> Put(int id, [FromBody] Order order)
        {
            _logger.LogInformation("Updating order {OrderId}", id);
            order.Id = id;
            var updated = await _repository.UpdateAsync(order);
            if (!updated)
            {
                _logger.LogWarning("Failed to update order {OrderId}: not found", id);
                return NotFound();
            }
            _logger.LogInformation("Successfully updated order {OrderId}", id);
            return Ok(order);
        }

        /// <summary>Finalizes/places a pending order cart.</summary>
        [HttpPost("{id:int}/place")]
        public async Task<ActionResult> PlaceOrder(int id)
        {
            _logger.LogInformation("Attempting to place order {OrderId}", id);
            var result = await _orderService.PlaceOrderAsync(id);

            switch (result)
            {
                case PlaceOrderResult.Placed:
                    _logger.LogInformation("Order {OrderId} successfully placed", id);
                    return Ok();
                case PlaceOrderResult.NotFound:
                    _logger.LogWarning("Place order failed for order {OrderId}: order not found", id);
                    return NotFound();
                case PlaceOrderResult.SlotUnavailable:
                    _logger.LogWarning("Place order failed for order {OrderId}: pickup slot no longer available", id);
                    return BadRequest(new ProblemDetails { Title = "The selected pickup slot is no longer available.", Status = StatusCodes.Status400BadRequest });
                case PlaceOrderResult.SlotFull:
                    _logger.LogWarning("Place order failed for order {OrderId}: pickup slot is full", id);
                    return Conflict(new ProblemDetails { Title = "The selected pickup slot is full. Please choose another time.", Status = StatusCodes.Status409Conflict });
                case PlaceOrderResult.AlreadyPlaced:
                    _logger.LogWarning("Place order failed for order {OrderId}: order has already been placed", id);
                    return Conflict(new ProblemDetails { Title = "Order has already been placed.", Status = StatusCodes.Status409Conflict });
                case PlaceOrderResult.PickupSlotRequired:
                    _logger.LogWarning("Place order failed for order {OrderId}: pickup slot is required", id);
                    return BadRequest(new ProblemDetails { Title = "A pickup slot is required to place an order.", Status = StatusCodes.Status400BadRequest });
                default:
                    _logger.LogError("Unhandled place order result {Result} for order {OrderId}", result, id);
                    throw new InvalidOperationException($"Unhandled result: {result}.");
            }
        }

        /// <summary>Soft-deletes an order by identifier.</summary>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            _logger.LogInformation("Deleting order {OrderId}", id);
            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
            {
                _logger.LogWarning("Failed to delete order {OrderId}: not found", id);
                return NotFound();
            }
            _logger.LogInformation("Successfully deleted order {OrderId}", id);
            return NoContent();
        }

        /// <summary>Retrieves all line items for an order.</summary>
        [HttpGet("{orderId:int}/lines")]
        public async Task<ActionResult<IEnumerable<OrderItem>>> GetLines(int orderId)
        {
            _logger.LogInformation("Retrieving line items for order {OrderId}", orderId);
            var lines = await _repository.GetLinesByOrderIdAsync(orderId);
            return Ok(lines);
        }

        /// <summary>Adds a new line item to an order.</summary>
        [HttpPost("{orderId:int}/lines")]
        public async Task<ActionResult<OrderItem>> AddLine(int orderId, [FromBody] OrderItem line)
        {
            _logger.LogInformation("Adding line item to order {OrderId}, MenuItemId: {MenuItemId}", orderId, line?.MenuItemId);
            var order = await _repository.GetByIdAsync(orderId);
            if (order == null)
            {
                _logger.LogWarning("Cannot add line item: order {OrderId} not found", orderId);
                return NotFound();
            }

            line!.OrderId = orderId;
            var created = await _repository.AddLineAsync(line);
            _logger.LogInformation("Added line item {LineItemId} to order {OrderId}", created.Id, orderId);
            return CreatedAtAction(nameof(OrderItemsController.GetById), "OrderItems", new { id = created.Id }, created);
        }
    }
}
