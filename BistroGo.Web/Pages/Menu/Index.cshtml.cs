using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Web.Cart;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BistroGo.Web.Pages.Menu;

// Page model for the menu page: shows available items (optionally filtered by category)
// and handles cart add/update/remove via standard POST and JSON endpoints.
public class IndexModel : PageModel
{
    private readonly IMenuItemRepository _menu;
    private readonly ICartService _cart;

    public IndexModel(IMenuItemRepository menu, ICartService cart)
    {
        _menu = menu;
        _cart = cart;
    }

    // View data: filtered menu, category dropdown options, and the active filter.
    public List<MenuItem> MenuItems { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public string? SelectedCategory { get; set; }

    // Cart snapshot for the page's own badge (populated in OnGet).
    public IReadOnlyList<OrderItem> OrderLines { get; private set; } = new List<OrderItem>();
    public decimal OrderTotal { get; private set; }


    // Loads available items, builds categories, applies the optional filter, and loads the cart badge.
    public async Task OnGetAsync(string? category)
    {
        SelectedCategory = category;

        // IsAvailable filters out items marked unavailable; repo already sorts by DisplayOrder then name.
        var all = (await _menu.GetAllAsync()).Where(m => m.IsAvailable).ToList(); // IsAvailable filters items markes as unavailable

        // Distinct() preserves first-seen order, so categories follow DisplayOrder (not alphabetical).
        Categories = all.Select(m => m.Category!.Name).Distinct().ToList();

        // No category = show all; otherwise filter to the selected category.
        MenuItems = string.IsNullOrEmpty(category)
            ? all
            : all.Where(m => m.Category!.Name == category).ToList();

        // Find (not FindOrCreate): viewing the menu must never create a cart.
        var owner = CartOwnerResolver.Find(HttpContext);
        var cart = owner == null ? null : await _cart.GetCartAsync(owner);
        OrderLines = cart?.Items.ToList() ?? new List<OrderItem>();
        OrderTotal = cart?.Total ?? 0m;
    }

    // Adds an item to the cart (creating the cart if needed) and redirects back, preserving the filter.
    public async Task<IActionResult> OnPostAddToOrderAsync(int menuItemId, int quantity, string? category)
    {
        // FindOrCreate: adding legitimately creates a cart owned by the user or guest cookie.
        var owner = CartOwnerResolver.FindOrCreate(HttpContext);

        // Guard against a missing/tampered hidden input.
        if (quantity < 1) quantity = 1;

        bool added = false;

        // Add one at a time so a mid-way failure (item unavailable) stops the loop.
        for (int i = 0; i < quantity; i++)
        {
            added = await _cart.AddItemAsync(owner, menuItemId);
            if (!added) break;
        }

        // Flash message via TempData (PRG pattern) to report success or unavailability.
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

    // Sets an absolute quantity for a line (cart modal -/+ buttons); returns updated cart JSON.
    public async Task<IActionResult> OnPostUpdateQuantityJsonAsync(int menuItemId, int quantity)
    {
        // Find, not FindOrCreate: nothing to update without a cart.
        var owner = CartOwnerResolver.Find(HttpContext);   // Find, not FindOrCreate: nothing to update without a cart
        if (owner != null)
            await _cart.SetQuantityAsync(owner, menuItemId, quantity);

        return await CartJsonAsync(owner);
    }

    // Removes a line entirely (cart modal ✕ button); returns updated cart JSON.
    public async Task<IActionResult> OnPostRemoveLineJsonAsync(int menuItemId)
    {
        var owner = CartOwnerResolver.Find(HttpContext);
        if (owner != null)
            await _cart.RemoveItemAsync(owner, menuItemId);

        return await CartJsonAsync(owner);
    }

    // Serializes the cart (lines, itemCount, total) into the JSON shape the modal script expects.
    private async Task<IActionResult> CartJsonAsync(CartOwner? owner)
    {
        // Null owner means no cart: return empty lines and a zero total.
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
            subtotal = lines.Sum(l => l.UnitPrice * l.Quantity)
        });
    }
}