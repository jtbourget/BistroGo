using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BistroGo.Api.Controllers
{
    /// <summary>
    /// RESTful API controller for directly managing order line items.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class OrderItemsController : ControllerBase
    {
        private readonly IOrderItemRepository _repository;
        private readonly ILogger<OrderItemsController> _logger;

        public OrderItemsController(IOrderItemRepository repository, ILogger<OrderItemsController>? logger = null)
        {
            _repository = repository;
            _logger = logger ?? NullLogger<OrderItemsController>.Instance;
        }

        /// <summary>Retrieves all non-archived order line items.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderItem>>> Get()
        {
            _logger.LogInformation("Retrieving all order items");
            var items = await _repository.GetAllAsync();
            return Ok(items);
        }

        /// <summary>Retrieves a line item by identifier.</summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<OrderItem>> GetById(int id)
        {
            _logger.LogInformation("Retrieving order item by id {OrderItemId}", id);
            var item = await _repository.GetByIdAsync(id);
            if (item == null)
            {
                _logger.LogWarning("Order item {OrderItemId} not found", id);
                return NotFound();
            }
            return Ok(item);
        }

        /// <summary>Retrieves all line items for a specific order identifier.</summary>
        [HttpGet("by-order/{orderId:int}")]
        public async Task<ActionResult<IEnumerable<OrderItem>>> GetByOrderId(int orderId)
        {
            _logger.LogInformation("Retrieving order items for order {OrderId}", orderId);
            var items = await _repository.GetByOrderIdAsync(orderId);
            return Ok(items);
        }

        /// <summary>Adds a new order line item.</summary>
        [HttpPost]
        public async Task<ActionResult<OrderItem>> Post([FromBody] OrderItem item)
        {
            _logger.LogInformation("Creating order item for order {OrderId}, MenuItemId: {MenuItemId}, Quantity: {Quantity}",
                item?.OrderId, item?.MenuItemId, item?.Quantity);
            var created = await _repository.AddAsync(item!);
            _logger.LogInformation("Created order item {OrderItemId} for order {OrderId}", created.Id, created.OrderId);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>Updates an existing order line item.</summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<OrderItem>> Put(int id, [FromBody] OrderItem item)
        {
            _logger.LogInformation("Updating order item {OrderItemId}", id);
            item.Id = id;
            var updated = await _repository.UpdateAsync(item);
            if (!updated)
            {
                _logger.LogWarning("Failed to update order item {OrderItemId}: not found", id);
                return NotFound();
            }
            _logger.LogInformation("Successfully updated order item {OrderItemId}", id);
            return Ok(item);
        }

        /// <summary>Soft-deletes a line item by identifier.</summary>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            _logger.LogInformation("Deleting order item {OrderItemId}", id);
            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
            {
                _logger.LogWarning("Failed to delete order item {OrderItemId}: not found", id);
                return NotFound();
            }
            _logger.LogInformation("Successfully deleted order item {OrderItemId}", id);
            return NoContent();
        }
    }
}
