using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BistroGo.Web.Pages.Menu;

public class MenuItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public string Category { get; set; } = "";
    public string? ImageUrl { get; set; }
}

public class IndexModel : PageModel
{
    public List<MenuItemDto> MenuItems { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public string? SelectedCategory { get; set; }

    public void OnGet(string? category)
    {
        SelectedCategory = category;
        LoadMenu(category);
    }

    public IActionResult OnPostAddToOrder(int menuItemId, string? category)
    {
        // TODO: Replace with real order logic later.
        // For now, find the item and stash a confirmation message.
        var all = GetAllMenuItems();
        var item = all.FirstOrDefault(m => m.Id == menuItemId);

        if (item != null)
        {
            // LATER: call the Api to add to the current open order
            TempData["Message"] = $"Added \"{item.Name}\" to your order.";
        }

        // Redirect back to the same filtered view so the user stays where they were.
        return RedirectToPage(new { category });
    }

    private void LoadMenu(string? category)
    {
        var all = GetAllMenuItems();
        Categories = all.Select(m => m.Category).Distinct().OrderBy(c => c).ToList();
        MenuItems = string.IsNullOrEmpty(category)
            ? all
            : all.Where(m => m.Category == category).ToList();
    }

    private List<MenuItemDto> GetAllMenuItems() => new()
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
}