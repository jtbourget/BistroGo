using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BistroGo.Api.Controllers
{
    /// <summary>
    /// RESTful API controller for managing menu categories.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly IMenuCategoryRepository _repository;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(IMenuCategoryRepository repository, ILogger<CategoriesController>? logger = null)
        {
            _repository = repository;
            _logger = logger ?? NullLogger<CategoriesController>.Instance;
        }

        /// <summary>Retrieves all active categories.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MenuCategory>>> Get()
        {
            _logger.LogInformation("Retrieving all active categories");
            var categories = await _repository.GetAllAsync();
            return Ok(categories);
        }

        /// <summary>Retrieves a category by identifier.</summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<MenuCategory>> GetById(int id)
        {
            _logger.LogInformation("Retrieving category by id {CategoryId}", id);
            var category = await _repository.GetByIdAsync(id);
            if (category == null)
            {
                _logger.LogWarning("Category {CategoryId} not found", id);
                return NotFound();
            }
            return Ok(category);
        }

        /// <summary>Creates a new menu category.</summary>
        [HttpPost]
        public async Task<ActionResult<MenuCategory>> Post([FromBody] MenuCategory category)
        {
            _logger.LogInformation("Creating menu category: {CategoryName}", category?.Name);
            var created = await _repository.AddAsync(category!);
            _logger.LogInformation("Created menu category {CategoryId} with name '{CategoryName}'", created.Id, created.Name);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>Updates an existing menu category.</summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<MenuCategory>> Put(int id, [FromBody] MenuCategory category)
        {
            _logger.LogInformation("Updating menu category {CategoryId}", id);
            category.Id = id;
            var updated = await _repository.UpdateAsync(category);
            if (!updated)
            {
                _logger.LogWarning("Failed to update category {CategoryId}: not found", id);
                return NotFound();
            }
            _logger.LogInformation("Successfully updated menu category {CategoryId}", id);
            return Ok(category);
        }

        /// <summary>Soft-deletes a category by identifier.</summary>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            _logger.LogInformation("Deleting menu category {CategoryId}", id);
            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
            {
                _logger.LogWarning("Failed to delete category {CategoryId}: not found", id);
                return NotFound();
            }
            _logger.LogInformation("Successfully deleted menu category {CategoryId}", id);
            return NoContent();
        }
    }
}
