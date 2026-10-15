using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Web.Pages.Inventory;      // still needed: OnPostAddToOrder uses MenuStore for now
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BistroGo.Web.Pages.Menu;

public class IndexModel : PageModel
{
    private readonly IMenuItemRepository _menu;

    public IndexModel(IMenuItemRepository menu)
    {
        _menu = menu;
    }

    public List<MenuItem> MenuItems { get; set; } = new();     // was List<MenuItemDto>
    public List<string> Categories { get; set; } = new();
    public string? SelectedCategory { get; set; }

    public async Task OnGetAsync(string? category)             // was: public void OnGet
    {
        SelectedCategory = category;

        // Already sorted by category DisplayOrder, then name.
        var all = (await _menu.GetAllAsync()).Where(m => m.IsAvailable).ToList(); // IsAvailable filters items markes as unavailable

        // Distinct() keeps first-seen order, so the dropdown follows DisplayOrder
        // (the old OrderBy(c => c) sorted alphabetically).
        Categories = all.Select(m => m.Category!.Name).Distinct().ToList();

        MenuItems = string.IsNullOrEmpty(category)
            ? all
            : all.Where(m => m.Category!.Name == category).ToList();
    }

    /// <summary>
    /// Handles the POST request when a user adds a menu item to their order.
    /// Increments the global CartCount and sets a success message in TempData.
    /// </summary>
    /// <param name="menuItemId">The unique identifier of the menu item to add.</param>
    /// <param name="category">The currently selected category to redirect back to.</param>
    /// <returns>A redirect to the current menu page, maintaining the selected category filter.</returns>
    public IActionResult OnPostAddToOrder(int menuItemId, string? category)
    {
        var item = MenuStore.Items.FirstOrDefault(m => m.Id == menuItemId);
        if (item != null)
        {
            // Increment the shopping cart ticker when a new item is added
            MenuStore.AddToCart(item);
            TempData["Message"] = $"Added \"{item.Name}\" to your order.";
        }
        return RedirectToPage(new { category });
    }
}