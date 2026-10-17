using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Web.Pages.Inventory;      // still needed: OnPostAddToOrder uses MenuStore for now
using BistroGo.Web.Stores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BistroGo.Web.Cart;

namespace BistroGo.Web.Pages.Menu;

public class IndexModel : PageModel
{
    private readonly IMenuItemRepository _menu;
    private readonly ICartService _cart;

    public IndexModel(IMenuItemRepository menu, ICartService cart)
    {
        _menu = menu;
        _cart = cart;
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
    /// Handles the POST when a user adds a menu item to their order.
    /// The cart is saved in the database, owned by the logged-in user or a guest cookie.
    /// </summary>
    /// <param name="menuItemId">The database id of the menu item to add.</param>
    /// <param name="category">The currently selected category to redirect back to.</param>
    public async Task<IActionResult> OnPostAddToOrderAsync(int menuItemId, string? category)
    {
        var owner = CartOwnerResolver.FindOrCreate(HttpContext);   // user from login, or guest cookie
        var added = await _cart.AddItemAsync(owner, menuItemId);

        if (added)
        {
            var item = await _menu.GetByIdAsync(menuItemId);
            TempData["Message"] = $"Added \"{item?.Name}\" to your order.";
        }
        else
        {
            TempData["Message"] = "Sorry, that item isn't available.";
        }

        return RedirectToPage(new { category });
    }

    public IActionResult OnPostUpdateOrderLine(int menuItemId, int quantity)
    {
        CurrentOrder.UpdateQuantity(menuItemId, quantity);
        TempData["Message"] = "Cart updated.";
        return RedirectToPage();
    }

    public IActionResult OnPostRemoveOrderLine(int menuItemId)
    {
        CurrentOrder.RemoveItem(menuItemId);
        TempData["Message"] = "Item removed from cart.";
        return RedirectToPage();
    }

    // NEW — JSON versions for the modal (no redirect, returns updated cart)
    public IActionResult OnPostUpdateQuantityJson(int menuItemId, int quantity)
    {
        CurrentOrder.UpdateQuantity(menuItemId, quantity);
        MenuStore.CartCount = CurrentOrder.TotalItems;   // keep sidebar and shopping cart number in sync
        return new JsonResult(BuildCartJson());
    }

    public IActionResult OnPostRemoveLineJson(int menuItemId)
    {
        CurrentOrder.RemoveItem(menuItemId);
        MenuStore.CartCount = CurrentOrder.TotalItems;   // keep sidebar and shopping cart number in sync
        return new JsonResult(BuildCartJson());
    }

    private object BuildCartJson() => new
    {
        lines = CurrentOrder.Lines.Select(l => new
        {
            menuItemId = l.MenuItemId,
            itemName = l.MenuItem?.Name ?? "",
            unitPrice = l.UnitPrice,
            quantity = l.Quantity,
            total = l.UnitPrice * l.Quantity
        }),
        itemCount = CurrentOrder.TotalItems,
        total = CurrentOrder.TotalPrice
    };
}