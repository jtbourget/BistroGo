using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Data.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BistroGo.Data.Repositories
{
    public class EfExpenseRepository : IExpenseRepository
    {
        private readonly BistroGoDBContext _db;
        private readonly ILogger<EfExpenseRepository> _logger;

        public EfExpenseRepository(BistroGoDBContext db, ILogger<EfExpenseRepository>? logger = null)
        {
            _db = db;
            _logger = logger ?? NullLogger<EfExpenseRepository>.Instance;
        }

        public async Task<List<Expense>> GetAllAsync()
        {
            _logger.LogDebug("Fetching all expenses");
            return await _db.Expenses
                .AsNoTracking()
                .Include(e => e.MenuItem)
                .OrderByDescending(e => e.ExpenseDate)
                .ToListAsync();
        }

        public async Task<Expense?> GetByIdAsync(int id)
        {
            _logger.LogDebug("Fetching expense with id {ExpenseId}", id);
            return await _db.Expenses
                .AsNoTracking()
                .Include(e => e.MenuItem)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<List<Expense>> GetByCategoryAsync(string category)
        {
            _logger.LogDebug("Fetching expenses in category '{ExpenseCategory}'", category);
            return await _db.Expenses
                .AsNoTracking()
                .Include(e => e.MenuItem)
                .Where(e => e.Category == category)
                .OrderByDescending(e => e.ExpenseDate)
                .ToListAsync();
        }

        public async Task<Expense> AddAsync(Expense expense)
        {
            _logger.LogInformation("Adding expense '{Description}' of amount {Amount}", expense.Description, expense.Amount);
            _db.Expenses.Add(expense);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Added expense with id {ExpenseId}", expense.Id);
            return expense;
        }

        public async Task<bool> UpdateAsync(Expense expense)
        {
            _logger.LogInformation("Updating expense {ExpenseId}", expense.Id);
            var existing = await _db.Expenses.FindAsync(expense.Id);
            if (existing == null)
            {
                _logger.LogWarning("Cannot update expense {ExpenseId}: not found", expense.Id);
                return false;
            }

            existing.Description = expense.Description;
            existing.Amount = expense.Amount;
            existing.Category = expense.Category;
            existing.ExpenseDate = expense.ExpenseDate;
            existing.MenuItemId = expense.MenuItemId;

            await _db.SaveChangesAsync();
            _logger.LogInformation("Successfully updated expense {ExpenseId}", expense.Id);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            _logger.LogInformation("Soft-deleting expense {ExpenseId}", id);
            var existing = await _db.Expenses.FindAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("Cannot delete expense {ExpenseId}: not found", id);
                return false;
            }

            existing.IsArchived = true;
            await _db.SaveChangesAsync();
            _logger.LogInformation("Successfully soft-deleted expense {ExpenseId}", id);
            return true;
        }
    }
}
