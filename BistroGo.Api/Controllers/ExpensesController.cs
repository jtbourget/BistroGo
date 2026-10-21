using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace BistroGo.Api.Controllers
{
    /// <summary>
    /// RESTful API controller for recording and managing restaurant operating expenses.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ExpensesController : ControllerBase
    {
        private readonly IExpenseRepository _repository;

        public ExpensesController(IExpenseRepository repository)
        {
            _repository = repository;
        }

        /// <summary>Retrieves all recorded expenses.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Expense>>> Get()
        {
            var expenses = await _repository.GetAllAsync();
            return Ok(expenses);
        }

        /// <summary>Retrieves an expense by identifier.</summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Expense>> GetById(int id)
        {
            var expense = await _repository.GetByIdAsync(id);
            if (expense == null)
            {
                return NotFound();
            }
            return Ok(expense);
        }

        /// <summary>Retrieves expenses filtered by category.</summary>
        [HttpGet("by-category/{category}")]
        public async Task<ActionResult<IEnumerable<Expense>>> GetByCategory(string category)
        {
            var expenses = await _repository.GetByCategoryAsync(category);
            return Ok(expenses);
        }

        /// <summary>Records a new expense.</summary>
        [HttpPost]
        public async Task<ActionResult<Expense>> Post([FromBody] Expense expense)
        {
            var created = await _repository.AddAsync(expense);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>Updates an existing expense entry.</summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<Expense>> Put(int id, [FromBody] Expense expense)
        {
            expense.Id = id;
            var updated = await _repository.UpdateAsync(expense);
            if (!updated)
            {
                return NotFound();
            }
            return Ok(expense);
        }

        /// <summary>Soft-deletes an expense entry by identifier.</summary>
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
