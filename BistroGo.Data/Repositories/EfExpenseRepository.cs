using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Data.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace BistroGo.Data.Repositories
{
    public class EfExpenseRepository : IExpenseRepository
    {
        private readonly BistroGoDBContext _db;

        public EfExpenseRepository(BistroGoDBContext db)
        {
            _db = db;
        }

        public async Task<List<Expense>> GetAllAsync()
        {
            return await _db.Expenses
                .AsNoTracking()
                .Include(e => e.MenuItem)
                .OrderByDescending(e => e.ExpenseDate)
                .ToListAsync();
        }

        public async Task<Expense?> GetByIdAsync(int id)
        {
            return await _db.Expenses
                .AsNoTracking()
                .Include(e => e.MenuItem)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<List<Expense>> GetByCategoryAsync(string category)
        {
            return await _db.Expenses
                .AsNoTracking()
                .Include(e => e.MenuItem)
                .Where(e => e.Category == category)
                .OrderByDescending(e => e.ExpenseDate)
                .ToListAsync();
        }

        public async Task<Expense> AddAsync(Expense expense)
        {
            _db.Expenses.Add(expense);
            await _db.SaveChangesAsync();
            return expense;
        }

        public async Task<bool> UpdateAsync(Expense expense)
        {
            var existing = await _db.Expenses.FindAsync(expense.Id);
            if (existing == null) return false;

            existing.Description = expense.Description;
            existing.Amount = expense.Amount;
            existing.Category = expense.Category;
            existing.ExpenseDate = expense.ExpenseDate;
            existing.MenuItemId = expense.MenuItemId;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _db.Expenses.FindAsync(id);
            if (existing == null) return false;

            existing.IsArchived = true;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
