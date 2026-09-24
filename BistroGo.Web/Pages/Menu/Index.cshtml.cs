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

    public IActionResult OnPostAddToOrder(int menuItemId, string? category)
    {
        var item = MenuStore.Items.FirstOrDefault(m => m.Id == menuItemId);
        if (item != null)
        {
            TempData["Message"] = $"Added \"{item.Name}\" to your order.";
        }
        return RedirectToPage(new { category });
    }
}