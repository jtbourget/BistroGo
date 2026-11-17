using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Data.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BistroGo.Data.Repositories
{
    public class EfCategoryRepository : IMenuCategoryRepository
    {
        private readonly BistroGoDBContext _db;
        private readonly ILogger<EfCategoryRepository> _logger;

        public EfCategoryRepository(BistroGoDBContext db, ILogger<EfCategoryRepository>? logger = null)
        {
            _db = db;
            _logger = logger ?? NullLogger<EfCategoryRepository>.Instance;
        }

        public async Task<List<MenuCategory>> GetAllAsync()
        {
            _logger.LogDebug("Fetching all active menu categories");
            return await _db.MenuCategories
                .AsNoTracking()
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<MenuCategory?> GetByIdAsync(int id)
        {
            _logger.LogDebug("Fetching menu category with id {CategoryId}", id);
            return await _db.MenuCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<MenuCategory?> GetByNameAsync(string name)
        {
            _logger.LogDebug("Fetching menu category with name '{CategoryName}'", name);
            return await _db.MenuCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
        }

        public async Task<MenuCategory> AddAsync(MenuCategory category)
        {
            _logger.LogInformation("Adding menu category '{CategoryName}'", category.Name);
            _db.MenuCategories.Add(category);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Added menu category '{CategoryName}' with id {CategoryId}", category.Name, category.Id);
            return category;
        }

        public async Task<bool> UpdateAsync(MenuCategory category)
        {
            _logger.LogInformation("Updating menu category {CategoryId}", category.Id);
            var existing = await _db.MenuCategories.FindAsync(category.Id);
            if (existing == null)
            {
                _logger.LogWarning("Cannot update category {CategoryId}: not found", category.Id);
                return false;
            }

            existing.Name = category.Name;
            existing.DisplayOrder = category.DisplayOrder;

            await _db.SaveChangesAsync();
            _logger.LogInformation("Successfully updated menu category {CategoryId}", category.Id);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            _logger.LogInformation("Soft-deleting menu category {CategoryId}", id);
            var existing = await _db.MenuCategories.FindAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("Cannot delete category {CategoryId}: not found", id);
                return false;
            }

            // Soft-delete via archive flag
            existing.IsArchived = true;
            await _db.SaveChangesAsync();
            _logger.LogInformation("Successfully soft-deleted menu category {CategoryId}", id);
            return true;
        }
    }
}
