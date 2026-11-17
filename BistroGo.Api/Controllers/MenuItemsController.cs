using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
        private readonly ILogger<MenuItemsController> _logger;

        public MenuItemsController(IMenuItemRepository repository, ILogger<MenuItemsController>? logger = null)
        {
            _repository = repository;
            _logger = logger ?? NullLogger<MenuItemsController>.Instance;
        }

        /// <summary>Retrieves all active menu items with associated categories.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MenuItem>>> Get()
        {
            _logger.LogInformation("Retrieving all active menu items");
            var items = await _repository.GetAllAsync();
            return Ok(items);
        }

        /// <summary>Retrieves a single menu item by identifier.</summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<MenuItem>> GetById(int id)
        {
            _logger.LogInformation("Retrieving menu item by id {MenuItemId}", id);
            var item = await _repository.GetByIdAsync(id);
            if (item == null)
            {
                _logger.LogWarning("Menu item {MenuItemId} not found", id);
                return NotFound();
            }
            return Ok(item);
        }

        /// <summary>Creates a new menu item.</summary>
        [HttpPost]
        public async Task<ActionResult<MenuItem>> Post([FromBody] MenuItem item)
        {
            _logger.LogInformation("Creating menu item: {Name}, Price: {Price}, CategoryId: {CategoryId}",
                item?.Name, item?.Price, item?.CategoryId);
            var created = await _repository.AddAsync(item!);
            _logger.LogInformation("Created menu item {MenuItemId} with name '{MenuItemName}'", created.Id, created.Name);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>Updates an existing menu item.</summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<MenuItem>> Put(int id, [FromBody] MenuItem item)
        {
            _logger.LogInformation("Updating menu item {MenuItemId}", id);
            item.Id = id;
            var updated = await _repository.UpdateAsync(item);
            if (!updated)
            {
                _logger.LogWarning("Failed to update menu item {MenuItemId}: not found", id);
                return NotFound();
            }
            _logger.LogInformation("Successfully updated menu item {MenuItemId}", id);
            return Ok(item);
        }

        /// <summary>Soft-deletes a menu item by identifier.</summary>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            _logger.LogInformation("Deleting menu item {MenuItemId}", id);
            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
            {
                _logger.LogWarning("Failed to delete menu item {MenuItemId}: not found", id);
                return NotFound();
            }
            _logger.LogInformation("Successfully deleted menu item {MenuItemId}", id);
            return NoContent();
        }
    }
}
