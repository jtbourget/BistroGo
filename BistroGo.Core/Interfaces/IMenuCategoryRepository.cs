using BistroGo.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BistroGo.Core.Interfaces
{
    public interface IMenuCategoryRepository
    {
        Task<List<MenuCategory>> GetAllAsync();
        Task<MenuCategory?> GetByIdAsync(int id);
        Task<MenuCategory?> GetByNameAsync(string name);
        Task<MenuCategory> AddAsync(MenuCategory category);
        Task<bool> UpdateAsync(MenuCategory category);
        Task<bool> DeleteAsync(int id);
    }
}
