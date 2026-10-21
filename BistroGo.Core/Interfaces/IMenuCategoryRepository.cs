using BistroGo.Core.Models;

namespace BistroGo.Core.Interfaces
{
    /// <summary>
    /// Contract for managing restaurant menu categories.
    /// </summary>
    public interface IMenuCategoryRepository
    {
        /// <summary>Retrieves all non-archived menu categories ordered by display order then name.</summary>
        Task<List<MenuCategory>> GetAllAsync();

        /// <summary>Retrieves a single category by identifier.</summary>
        Task<MenuCategory?> GetByIdAsync(int id);

        /// <summary>Retrieves a category matching the specified name.</summary>
        Task<MenuCategory?> GetByNameAsync(string name);

        /// <summary>Adds a new category to the database.</summary>
        Task<MenuCategory> AddAsync(MenuCategory category);

        /// <summary>Updates name or display order of an existing category.</summary>
        Task<bool> UpdateAsync(MenuCategory category);

        /// <summary>Soft-deletes a category by setting IsArchived = true.</summary>
        Task<bool> DeleteAsync(int id);
    }
}
