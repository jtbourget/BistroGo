using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Web.Cart;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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
    public IReadOnlyList<OrderItem> OrderLines { get; private set; } = new List<OrderItem>();
    public decimal OrderTotal { get; private set; }

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

        // Cart for the Menu page's own badge. Find (not FindOrCreate): looking must never create a cart.
        var owner = CartOwnerResolver.Find(HttpContext);
        var cart = owner == null ? null : await _cart.GetCartAsync(owner);
        OrderLines = cart?.Items.ToList() ?? new List<OrderItem>();
        OrderTotal = cart?.Total ?? 0m;
    }

    /// <summary>
    /// Handles the POST when a user adds a menu item to their order.
    /// The cart is saved in the database, owned by the logged-in user or a guest cookie.
    /// </summary>
    /// <param name="menuItemId">The database id of the menu item to add.</param>
    /// <param name="category">The currently selected category to redirect back to.</param>
    public async Task<IActionResult> OnPostAddToOrderAsync(int menuItemId, int quantity, string? category)
    {
        var owner = CartOwnerResolver.FindOrCreate(HttpContext);

        // Ensure quantity is at least 1 (in case the hidden input fails)
        if (quantity < 1) quantity = 1;

        bool added = false;

        // Loop and call the service for the requested quantity
        for (int i = 0; i < quantity; i++)
        {
            added = await _cart.AddItemAsync(owner, menuItemId);

            // If the item becomes unavailable or fails to add, stop the loop
            if (!added) break;
        }

        if (added)
        {
            var item = await _menu.GetByIdAsync(menuItemId);

            if (quantity > 1)
                TempData["Message"] = $"Added {quantity} x \"{item?.Name}\" to your order.";
            else
                TempData["Message"] = $"Added \"{item?.Name}\" to your order.";
        }
        else
        {
            TempData["Message"] = "Sorry, that item isn't available.";
        }

        return RedirectToPage(new { category });
    }

    // The cart modal's -/+ and ✕ buttons (JavaScript in _Layout) POST here and redraw from the JSON.
    public async Task<IActionResult> OnPostUpdateQuantityJsonAsync(int menuItemId, int quantity)
    {
        var owner = CartOwnerResolver.Find(HttpContext);   // Find, not FindOrCreate: nothing to update without a cart
        if (owner != null)
            await _cart.SetQuantityAsync(owner, menuItemId, quantity);

        return await CartJsonAsync(owner);
    }

    public async Task<IActionResult> OnPostRemoveLineJsonAsync(int menuItemId)
    {
        var owner = CartOwnerResolver.Find(HttpContext);
        if (owner != null)
            await _cart.RemoveItemAsync(owner, menuItemId);

        return await CartJsonAsync(owner);
    }

    // Same JSON shape the script already expects (lines / itemCount / total).
    private async Task<IActionResult> CartJsonAsync(CartOwner? owner)
    {
        var cart = owner == null ? null : await _cart.GetCartAsync(owner);
        var lines = cart?.Items.OrderBy(i => i.MenuItem!.Name).ToList() ?? new List<OrderItem>();

        return new JsonResult(new
        {
            lines = lines.Select(l => new
            {
                menuItemId = l.MenuItemId,
                itemName = l.MenuItem?.Name ?? "",
                unitPrice = l.UnitPrice,
                quantity = l.Quantity,
                total = l.UnitPrice * l.Quantity
            }),
            itemCount = lines.Sum(l => l.Quantity),
            total = cart?.Total ?? 0m
        });
    }
}