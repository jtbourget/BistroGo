using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
        private readonly ILogger<ExpensesController> _logger;

        public ExpensesController(IExpenseRepository repository, ILogger<ExpensesController>? logger = null)
        {
            _repository = repository;
            _logger = logger ?? NullLogger<ExpensesController>.Instance;
        }

        /// <summary>Retrieves all recorded expenses.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Expense>>> Get()
        {
            _logger.LogInformation("Retrieving all recorded expenses");
            var expenses = await _repository.GetAllAsync();
            return Ok(expenses);
        }

        /// <summary>Retrieves an expense by identifier.</summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Expense>> GetById(int id)
        {
            _logger.LogInformation("Retrieving expense by id {ExpenseId}", id);
            var expense = await _repository.GetByIdAsync(id);
            if (expense == null)
            {
                _logger.LogWarning("Expense {ExpenseId} not found", id);
                return NotFound();
            }
            return Ok(expense);
        }

        /// <summary>Retrieves expenses filtered by category.</summary>
        [HttpGet("by-category/{category}")]
        public async Task<ActionResult<IEnumerable<Expense>>> GetByCategory(string category)
        {
            _logger.LogInformation("Retrieving expenses by category: {ExpenseCategory}", category);
            var expenses = await _repository.GetByCategoryAsync(category);
            return Ok(expenses);
        }

        /// <summary>Records a new expense.</summary>
        [HttpPost]
        public async Task<ActionResult<Expense>> Post([FromBody] Expense expense)
        {
            _logger.LogInformation("Recording new expense: {Description}, Amount: {Amount}, Category: {Category}",
                expense?.Description, expense?.Amount, expense?.Category);
            var created = await _repository.AddAsync(expense!);
            _logger.LogInformation("Recorded expense {ExpenseId} successfully", created.Id);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>Updates an existing expense entry.</summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<Expense>> Put(int id, [FromBody] Expense expense)
        {
            _logger.LogInformation("Updating expense {ExpenseId}", id);
            expense.Id = id;
            var updated = await _repository.UpdateAsync(expense);
            if (!updated)
            {
                _logger.LogWarning("Failed to update expense {ExpenseId}: not found", id);
                return NotFound();
            }
            _logger.LogInformation("Successfully updated expense {ExpenseId}", id);
            return Ok(expense);
        }

        /// <summary>Soft-deletes an expense entry by identifier.</summary>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            _logger.LogInformation("Deleting expense {ExpenseId}", id);
            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
            {
                _logger.LogWarning("Failed to delete expense {ExpenseId}: not found", id);
                return NotFound();
            }
            _logger.LogInformation("Successfully deleted expense {ExpenseId}", id);
            return NoContent();
        }
    }
}
