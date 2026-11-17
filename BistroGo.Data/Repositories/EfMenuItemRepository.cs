using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Data.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BistroGo.Data.Repositories
{
    /// <summary>
    /// Menu items stored in the database (MenuItems table), via EF Core.
    /// Replaces InMemoryMenuItemRepository / MenuStore.Items as the source of truth.
    /// </summary>
    public class EfMenuItemRepository : IMenuItemRepository
    {
        private readonly BistroGoDBContext _db;
        private readonly ILogger<EfMenuItemRepository> _logger;

        public EfMenuItemRepository(BistroGoDBContext db, ILogger<EfMenuItemRepository>? logger = null)
        {
            _db = db;
            _logger = logger ?? NullLogger<EfMenuItemRepository>.Instance;
        }

        public async Task<List<MenuItem>> GetAllAsync()
        {
            _logger.LogDebug("Fetching all menu items");
            return await _db.MenuItems
                .AsNoTracking()
                .Include(i => i.Category)
                .OrderBy(i => i.Category!.DisplayOrder)
                .ThenBy(i => i.Name)
                .ToListAsync();
        }

        public async Task<MenuItem?> GetByIdAsync(int id)
        {
            _logger.LogDebug("Fetching menu item with id {MenuItemId}", id);
            return await _db.MenuItems
                .AsNoTracking()
                .Include(i => i.Category)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<MenuItem> AddAsync(MenuItem item)
        {
            _logger.LogInformation("Adding menu item '{MenuItemName}', Price: {Price}", item.Name, item.Price);
            _db.MenuItems.Add(item);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Added menu item '{MenuItemName}' with id {MenuItemId}", item.Name, item.Id);
            return item;
        }

        public async Task<bool> UpdateAsync(MenuItem item)
        {
            _logger.LogInformation("Updating menu item {MenuItemId}", item.Id);
            var existing = await _db.MenuItems.FindAsync(item.Id);
            if (existing == null)
            {
                _logger.LogWarning("Cannot update menu item {MenuItemId}: not found", item.Id);
                return false;
            }

            existing.Name = item.Name;
            existing.Description = item.Description;
            existing.Price = item.Price;
            existing.CategoryId = item.CategoryId;
            existing.ImageUrl = item.ImageUrl;
            existing.IsAvailable = item.IsAvailable;

            await _db.SaveChangesAsync();
            _logger.LogInformation("Successfully updated menu item {MenuItemId}", item.Id);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            _logger.LogInformation("Soft-deleting menu item {MenuItemId}", id);
            var existing = await _db.MenuItems.FindAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("Cannot delete menu item {MenuItemId}: not found", id);
                return false;
            }

            // Soft-delete via archive flag
            existing.IsArchived = true;
            existing.IsAvailable = false;
            await _db.SaveChangesAsync();
            _logger.LogInformation("Successfully soft-deleted menu item {MenuItemId}", id);
            return true;
        }
    }
}
