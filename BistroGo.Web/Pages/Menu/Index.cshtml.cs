using BistroGo.Web.Pages.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BistroGo.Web.Pages.Menu;

public class IndexModel : PageModel
{
    public List<MenuItemDto> MenuItems { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public string? SelectedCategory { get; set; }

    public void OnGet(string? category)
    {
        SelectedCategory = category;

        // TODO: replace with Api call to GET /api/menuitems
        var all = MenuStore.Items;

        Categories = all.Select(m => m.Category).Distinct().OrderBy(c => c).ToList();

        MenuItems = string.IsNullOrEmpty(category)
            ? all.ToList()
            : all.Where(m => m.Category == category).ToList();
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