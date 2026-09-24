using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;

namespace BistroGo.Data.Repositories;

public class InMemoryMenuCategoryRepository : IMenuCategoryRepository
{
    private static readonly List<MenuCategory> _categories = new()
    {
        new() { Id = 1, Name = "Appetizer", DisplayOrder = 1 },
        new() { Id = 2, Name = "Entree",    DisplayOrder = 2 },
        new() { Id = 3, Name = "Dessert",   DisplayOrder = 3 },
        new() { Id = 4, Name = "Drink",     DisplayOrder = 4 },
    };

    private static int _nextId = 100;

    public Task<List<MenuCategory>> GetAllAsync() =>
        Task.FromResult(_categories.OrderBy(c => c.DisplayOrder).ToList());

    public Task<MenuCategory?> GetByIdAsync(int id) =>
        Task.FromResult(_categories.FirstOrDefault(c => c.Id == id));

    public Task<MenuCategory?> GetByNameAsync(string name) =>
        Task.FromResult(_categories.FirstOrDefault(c =>
            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)));

    public Task<MenuCategory> AddAsync(MenuCategory category)
    {
        if (_categories.Any(c =>
            string.Equals(c.Name, category.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"Category \"{category.Name}\" already exists.");
        }

        category.Id = _nextId++;
        if (category.DisplayOrder <= 0)
        {
            category.DisplayOrder = _categories.Count == 0
                ? 1
                : _categories.Max(c => c.DisplayOrder) + 1;
        }

        _categories.Add(category);
        return Task.FromResult(category);
    }

    public Task<bool> UpdateAsync(MenuCategory category)
    {
        var existing = _categories.FirstOrDefault(c => c.Id == category.Id);
        if (existing == null) return Task.FromResult(false);

        if (_categories.Any(c =>
            c.Id != category.Id &&
            string.Equals(c.Name, category.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"Another category named \"{category.Name}\" already exists.");
        }

        existing.Name = category.Name;
        existing.DisplayOrder = category.DisplayOrder;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(int id)
    {
        var existing = _categories.FirstOrDefault(c => c.Id == id);
        return Task.FromResult(existing != null && _categories.Remove(existing));
    }
}