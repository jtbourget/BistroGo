using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;

namespace BistroGo.Data.Repositories;

public class InMemoryMenuItemRepository : IMenuItemRepository
{
    private static readonly List<MenuItem> _items = new()
    {
        new() { Id = 1, Name = "Bruschetta",      Description = "Grilled bread, tomatoes, basil",  Price = 8.50m,  Category = "Appetizer" },
        new() { Id = 2, Name = "Caesar Salad",     Description = "Romaine, parmesan, croutons",     Price = 10.00m, Category = "Appetizer" },
        new() { Id = 3, Name = "Margherita Pizza", Description = "Tomato, mozzarella, basil",       Price = 14.00m, Category = "Entree"    },
        new() { Id = 4, Name = "Grilled Salmon",   Description = "Lemon butter, seasonal veg",      Price = 22.00m, Category = "Entree"    },
        new() { Id = 5, Name = "Ribeye Steak",     Description = "12oz, garlic butter",             Price = 28.00m, Category = "Entree"    },
        new() { Id = 6, Name = "Tiramisu",         Description = "Classic Italian dessert",         Price = 9.00m,  Category = "Dessert"   },
        new() { Id = 7, Name = "Espresso",         Description = "Double shot",                     Price = 4.00m,  Category = "Drink"     },
        new() { Id = 8, Name = "House Red",        Description = "Glass of Cabernet",               Price = 11.00m, Category = "Drink"     },
    };

    private static int _nextId = 100;

    public Task<List<MenuItem>> GetAllAsync() =>
        Task.FromResult(_items.ToList());

    public Task<MenuItem?> GetByIdAsync(int id) =>
        Task.FromResult(_items.FirstOrDefault(i => i.Id == id));

    public Task<MenuItem> AddAsync(MenuItem item)
    {
        item.Id = _nextId++;
        _items.Add(item);
        return Task.FromResult(item);
    }

    public Task<bool> UpdateAsync(MenuItem item)
    {
        var existing = _items.FirstOrDefault(i => i.Id == item.Id);
        if (existing == null) return Task.FromResult(false);

        existing.Name = item.Name;
        existing.Description = item.Description;
        existing.Price = item.Price;
        existing.Category = item.Category;
        existing.ImageUrl = item.ImageUrl;

        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(int id)
    {
        var existing = _items.FirstOrDefault(i => i.Id == id);
        return Task.FromResult(existing != null && _items.Remove(existing));
    }
}