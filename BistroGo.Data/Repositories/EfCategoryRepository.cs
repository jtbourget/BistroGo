using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Data.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace BistroGo.Data.Repositories
{
    public class EfCategoryRepository : IMenuCategoryRepository
    {
        private readonly BistroGoDBContext _db;

        public EfCategoryRepository(BistroGoDBContext db)
        {
            _db = db;
        }

        public async Task<List<MenuCategory>> GetAllAsync()
        {
            return await _db.MenuCategories
                .AsNoTracking()
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<MenuCategory?> GetByIdAsync(int id)
        {
            return await _db.MenuCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<MenuCategory?> GetByNameAsync(string name)
        {
            return await _db.MenuCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
        }

        public async Task<MenuCategory> AddAsync(MenuCategory category)
        {
            _db.MenuCategories.Add(category);
            await _db.SaveChangesAsync();
            return category;
        }

        public async Task<bool> UpdateAsync(MenuCategory category)
        {
            var existing = await _db.MenuCategories.FindAsync(category.Id);
            if (existing == null) return false;

            existing.Name = category.Name;
            existing.DisplayOrder = category.DisplayOrder;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _db.MenuCategories.FindAsync(id);
            if (existing == null) return false;

            // Soft-delete via archive flag
            existing.IsArchived = true;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
