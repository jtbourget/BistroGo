using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using Microsoft.AspNetCore.Mvc;

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

        public CategoriesController(IMenuCategoryRepository repository)
        {
            _repository = repository;
        }

        /// <summary>Retrieves all active categories.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MenuCategory>>> Get()
        {
            var categories = await _repository.GetAllAsync();
            return Ok(categories);
        }

        /// <summary>Retrieves a category by identifier.</summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<MenuCategory>> GetById(int id)
        {
            var category = await _repository.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return Ok(category);
        }

        /// <summary>Creates a new menu category.</summary>
        [HttpPost]
        public async Task<ActionResult<MenuCategory>> Post([FromBody] MenuCategory category)
        {
            var created = await _repository.AddAsync(category);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>Updates an existing menu category.</summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<MenuCategory>> Put(int id, [FromBody] MenuCategory category)
        {
            category.Id = id;
            var updated = await _repository.UpdateAsync(category);
            if (!updated)
            {
                return NotFound();
            }
            return Ok(category);
        }

        /// <summary>Soft-deletes a category by identifier.</summary>
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
