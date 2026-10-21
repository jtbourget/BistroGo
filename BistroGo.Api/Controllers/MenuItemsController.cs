using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace BistroGo.Api.Controllers
{
    /// <summary>
    /// RESTful API controller for managing restaurant menu items.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MenuItemsController : ControllerBase
    {
        private readonly IMenuItemRepository _repository;

        public MenuItemsController(IMenuItemRepository repository)
        {
            _repository = repository;
        }

        /// <summary>Retrieves all active menu items with associated categories.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MenuItem>>> Get()
        {
            var items = await _repository.GetAllAsync();
            return Ok(items);
        }

        /// <summary>Retrieves a single menu item by identifier.</summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<MenuItem>> GetById(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            return Ok(item);
        }

        /// <summary>Creates a new menu item.</summary>
        [HttpPost]
        public async Task<ActionResult<MenuItem>> Post([FromBody] MenuItem item)
        {
            var created = await _repository.AddAsync(item);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>Updates an existing menu item.</summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<MenuItem>> Put(int id, [FromBody] MenuItem item)
        {
            item.Id = id;
            var updated = await _repository.UpdateAsync(item);
            if (!updated)
            {
                return NotFound();
            }
            return Ok(item);
        }

        /// <summary>Soft-deletes a menu item by identifier.</summary>
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
