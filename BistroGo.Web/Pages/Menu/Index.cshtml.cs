using BistroGo.Core.Models;
using BistroGo.Web.Pages.Inventory;
using BistroGo.Web.Stores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BistroGo.Web.Pages.Menu;

public class IndexModel : PageModel
{
    public List<MenuItemDto> MenuItems { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public string? SelectedCategory { get; set; }

    // ── Cart data exposed to the view ──────────────────────────────
    // TODO: Replace with GET /api/orders/current when the Api exists
    public IReadOnlyList<OrderItem> OrderLines => CurrentOrder.Lines;
    public decimal OrderTotal => CurrentOrder.TotalPrice;

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
        var dto = MenuStore.Items.FirstOrDefault(m => m.Id == menuItemId);
        if (dto != null)
        {
            // ── 1. Write to MenuStore (powers the sidebar badge + layout modal) ──
            MenuStore.AddToCart(dto);

            // ── 2. Write to CurrentOrder (powers the Menu page badge + cart modal) ──
            CurrentOrder.AddItem(new MenuItem
            {
                Id = dto.Id,
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                ImageUrl = dto.ImageUrl
            });

            TempData["Message"] = $"Added \"{dto.Name}\" to your cart.";
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