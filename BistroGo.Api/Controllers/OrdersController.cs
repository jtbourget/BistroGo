using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using Microsoft.AspNetCore.Mvc;

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

        public OrdersController(IOrderRepository repository)
        {
            _repository = repository;
        }

        /// <summary>Retrieves all orders, optionally filtered by user ID.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> Get([FromQuery] string? userId = null)
        {
            if (!string.IsNullOrEmpty(userId))
            {
                var userOrders = await _repository.GetByUserIdAsync(userId);
                return Ok(userOrders);
            }
            var orders = await _repository.GetAllAsync();
            return Ok(orders);
        }

        /// <summary>Retrieves an order by identifier with status and line items.</summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Order>> GetById(int id)
        {
            var order = await _repository.GetByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            return Ok(order);
        }

        /// <summary>Retrieves all orders for a specific user ID.</summary>
        [HttpGet("by-user/{userId}")]
        public async Task<ActionResult<IEnumerable<Order>>> GetByUserId(string userId)
        {
            var orders = await _repository.GetByUserIdAsync(userId);
            return Ok(orders);
        }

        /// <summary>Creates a new order.</summary>
        [HttpPost]
        public async Task<ActionResult<Order>> Post([FromBody] Order order)
        {
            var created = await _repository.AddAsync(order);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>Updates order details.</summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<Order>> Put(int id, [FromBody] Order order)
        {
            order.Id = id;
            var updated = await _repository.UpdateAsync(order);
            if (!updated)
            {
                return NotFound();
            }
            return Ok(order);
        }

        /// <summary>Finalizes/places a pending order cart.</summary>
        [HttpPost("{id:int}/place")]
        public async Task<ActionResult> PlaceOrder(int id)
        {
            var placed = await _repository.PlaceOrderAsync(id);
            if (!placed)
            {
                return BadRequest("Order could not be placed.");
            }
            return Ok();
        }

        /// <summary>Soft-deletes an order by identifier.</summary>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound();
            }
            return NoContent();
        }

        /// <summary>Retrieves all line items for an order.</summary>
        [HttpGet("{orderId:int}/lines")]
        public async Task<ActionResult<IEnumerable<OrderItem>>> GetLines(int orderId)
        {
            var lines = await _repository.GetLinesByOrderIdAsync(orderId);
            return Ok(lines);
        }

        /// <summary>Adds a new line item to an order.</summary>
        [HttpPost("{orderId:int}/lines")]
        public async Task<ActionResult<OrderItem>> AddLine(int orderId, [FromBody] OrderItem line)
        {
            line.OrderId = orderId;
            var created = await _repository.AddLineAsync(line);
            return Ok(created);
        }
    }
}
