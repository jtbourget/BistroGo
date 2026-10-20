using BistroGo.Core.Models;

namespace BistroGo.Core.Interfaces
{
    /// <summary>
    /// Contract for managing restaurant menu items.
    /// </summary>
    public interface IMenuItemRepository
    {
        /// <summary>Retrieves all non-archived menu items ordered by category and name.</summary>
        Task<List<MenuItem>> GetAllAsync();

        /// <summary>Retrieves a menu item by identifier with its category loaded.</summary>
        Task<MenuItem?> GetByIdAsync(int id);

        /// <summary>Persists a new menu item.</summary>
        Task<MenuItem> AddAsync(MenuItem item);

        /// <summary>Updates name, description, price, category, or availability of an item.</summary>
        Task<bool> UpdateAsync(MenuItem item);

        /// <summary>Soft-deletes a menu item by setting IsArchived = true and IsAvailable = false.</summary>
        Task<bool> DeleteAsync(int id);
    }
}
