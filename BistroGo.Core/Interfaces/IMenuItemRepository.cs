using BistroGo.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BistroGo.Core.Interfaces
{
    public interface IMenuItemRepository
    {
        Task<List<MenuItem>> GetAllAsync();
        Task<MenuItem?> GetByIdAsync(int id);
        Task<MenuItem> AddAsync(MenuItem item);
        Task<bool> UpdateAsync(MenuItem item);
        Task<bool> DeleteAsync(int id);
    }
}
