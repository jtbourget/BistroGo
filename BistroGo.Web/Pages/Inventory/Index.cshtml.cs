using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BistroGo.Web.Pages.Inventory;

/// <summary>
/// Simple in-memory store shared by Inventory and Menu pages.
/// LATER: This will be replaced by Api calls.
/// </summary>
public static class MenuStore
{
    private static int _nextItemId = 100;
    private static int _nextCategoryId = 100;

    public static List<MenuItemDto> Items { get; } = new()
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

    public static List<CategoryDto> Categories { get; } = new()
    {
        new() { Id = 1, Name = "Appetizer" },
        new() { Id = 2, Name = "Entree"    },
        new() { Id = 3, Name = "Dessert"   },
        new() { Id = 4, Name = "Drink"     },
    };

    public static MenuItemDto AddItem(MenuItemDto item)
    {
        item.Id = _nextItemId++;
        Items.Add(item);
        return item;
    }

    public static bool DeleteItem(int id)
    {
        var existing = Items.FirstOrDefault(i => i.Id == id);
        return existing != null && Items.Remove(existing);
    }

    public static CategoryDto? AddCategory(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        if (Categories.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
            return null;

        var category = new CategoryDto { Id = _nextCategoryId++, Name = name.Trim() };
        Categories.Add(category);
        return category;
    }

    public static bool DeleteCategory(int id)
    {
        var existing = Categories.FirstOrDefault(c => c.Id == id);
        if (existing == null) return false;

        // Also remove items in that category (or reassign — pick your policy)
        Items.RemoveAll(i => i.Category == existing.Name);
        return Categories.Remove(existing);
    }
}

public class MenuItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public string Category { get; set; } = "";
    public string? ImageUrl { get; set; }
}

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class IndexModel : PageModel
{
    public List<MenuItemDto> Items { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();

    [BindProperty]
    public NewItemInput NewItem { get; set; } = new();

    [BindProperty]
    public string? NewCategoryName { get; set; }

    public void OnGet()
    {
        LoadData();
    }

    public IActionResult OnPostAddItem()
    {
        if (!ModelState.IsValid)
        {
            LoadData();
            // Keep the modal open on validation error
            ViewData["OpenModal"] = true;
            return Page();
        }

        // TODO: replace with Api call to POST /api/menuitems
        MenuStore.AddItem(new MenuItemDto
        {
            Name = NewItem.Name.Trim(),
            Description = NewItem.Description.Trim(),
            Price = NewItem.Price,
            Category = NewItem.Category,
            ImageUrl = string.IsNullOrWhiteSpace(NewItem.ImageUrl) ? null : NewItem.ImageUrl.Trim()
        });

        TempData["Message"] = $"Added \"{NewItem.Name}\" to the menu.";
        return RedirectToPage();
    }

    public IActionResult OnPostAddCategory()
    {
        if (!string.IsNullOrWhiteSpace(NewCategoryName))
        {
            // TODO: replace with Api call to POST /api/categories
            var created = MenuStore.AddCategory(NewCategoryName);

            if (created == null)
                TempData["Error"] = $"Category \"{NewCategoryName}\" already exists.";
            else
                TempData["Message"] = $"Added category \"{created.Name}\".";
        }

        return RedirectToPage();
    }

    public IActionResult OnPostDeleteItem(int id)
    {
        // TODO: replace with Api call to DELETE /api/menuitems/{id}
        if (MenuStore.DeleteItem(id))
            TempData["Message"] = "Item removed.";
        return RedirectToPage();
    }

    public IActionResult OnPostDeleteCategory(int id)
    {
        // TODO: replace with Api call to DELETE /api/categories/{id}
        if (MenuStore.DeleteCategory(id))
            TempData["Message"] = "Category removed (and its items).";
        return RedirectToPage();
    }

    private void LoadData()
    {
        Items = MenuStore.Items.OrderBy(i => i.Category).ThenBy(i => i.Name).ToList();
        Categories = MenuStore.Categories.OrderBy(c => c.Name).ToList();
    }

    public class NewItemInput
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
        public string Category { get; set; } = "";
        public string? ImageUrl { get; set; }
    }
}