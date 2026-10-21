using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using Microsoft.AspNetCore.Mvc;

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

        public OrderItemsController(IOrderItemRepository repository)
        {
            _repository = repository;
        }

        /// <summary>Retrieves all non-archived order line items.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderItem>>> Get()
        {
            var items = await _repository.GetAllAsync();
            return Ok(items);
        }

        /// <summary>Retrieves a line item by identifier.</summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<OrderItem>> GetById(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            return Ok(item);
        }

        /// <summary>Retrieves all line items for a specific order identifier.</summary>
        [HttpGet("by-order/{orderId:int}")]
        public async Task<ActionResult<IEnumerable<OrderItem>>> GetByOrderId(int orderId)
        {
            var items = await _repository.GetByOrderIdAsync(orderId);
            return Ok(items);
        }

        /// <summary>Adds a new order line item.</summary>
        [HttpPost]
        public async Task<ActionResult<OrderItem>> Post([FromBody] OrderItem item)
        {
            var created = await _repository.AddAsync(item);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>Updates an existing order line item.</summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<OrderItem>> Put(int id, [FromBody] OrderItem item)
        {
            item.Id = id;
            var updated = await _repository.UpdateAsync(item);
            if (!updated)
            {
                return NotFound();
            }
            return Ok(item);
        }

        /// <summary>Soft-deletes a line item by identifier.</summary>
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
    }
}
