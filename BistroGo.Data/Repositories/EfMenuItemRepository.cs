using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Data.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace BistroGo.Data.Repositories
{
    /// <summary>
    /// Menu items stored in the database (MenuItems table), via EF Core.
    /// Replaces InMemoryMenuItemRepository / MenuStore.Items as the source of truth.
    /// </summary>
    public class EfMenuItemRepository : IMenuItemRepository
    {
        private readonly BistroGoDBContext _db;

        public EfMenuItemRepository(BistroGoDBContext db)
        {
            _db = db;
        }

        public async Task<List<MenuItem>> GetAllAsync()
        {
            return await _db.MenuItems
                .AsNoTracking() // Only reads items, does not edit them
                .Include(i => i.Category)
                .OrderBy(i => i.Category!.DisplayOrder)
                .ThenBy(i => i.Name)
                .ToListAsync();
        }

        public async Task<MenuItem?> GetByIdAsync(int id) // ID can return a null menu item, to ensure error message
        {
            return await _db.MenuItems
                .AsNoTracking()
                .Include(i => i.Category)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<MenuItem> AddAsync(MenuItem item)
        {
            _db.MenuItems.Add(item);
            await _db.SaveChangesAsync();
            return item;            // item.Id is now filled in by the database
        }

        public async Task<bool> UpdateAsync(MenuItem item)
        {
            var existing = await _db.MenuItems.FindAsync(item.Id);
            if (existing == null) return false;

            existing.Name = item.Name;
            existing.Description = item.Description;
            existing.Price = item.Price;
            existing.CategoryId = item.CategoryId;
            existing.ImageUrl = item.ImageUrl;
            existing.IsAvailable = item.IsAvailable;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _db.MenuItems.FindAsync(id);
            if (existing == null) return false;

            _db.MenuItems.Remove(existing);
            try
            {
                await _db.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                // FK Restrict: the item appears on an order. Retire it instead
                // (IsAvailable = false) so order history stays intact.
                return false;
            }
        }
    }
}