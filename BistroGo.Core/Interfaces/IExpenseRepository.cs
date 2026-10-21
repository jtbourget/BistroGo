using BistroGo.Core.Models;

namespace BistroGo.Core.Interfaces
{
    /// <summary>
    /// Contract for recording and auditing restaurant operating expenses.
    /// </summary>
    public interface IExpenseRepository
    {
        /// <summary>Retrieves all recorded non-archived expenses ordered by date descending.</summary>
        Task<List<Expense>> GetAllAsync();

        /// <summary>Retrieves a single expense entry by identifier.</summary>
        Task<Expense?> GetByIdAsync(int id);

        /// <summary>Retrieves expenses filtered by category (e.g., Supplies, Ingredients, Utilities).</summary>
        Task<List<Expense>> GetByCategoryAsync(string category);

        /// <summary>Records a new operating expense.</summary>
        Task<Expense> AddAsync(Expense expense);

        /// <summary>Updates an existing expense entry.</summary>
        Task<bool> UpdateAsync(Expense expense);

        /// <summary>Soft-deletes an expense entry by setting IsArchived = true.</summary>
        Task<bool> DeleteAsync(int id);
    }
}
