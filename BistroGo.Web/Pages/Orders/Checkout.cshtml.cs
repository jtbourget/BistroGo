using BistroGo.Core.Models;
using BistroGo.Web.Stores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BistroGo.Web.Pages.Orders;

public class CheckoutModel : PageModel
{
    // ═══════════════════════════════════════════════════════════════════
    // DATA TO POPULATE FROM THE API / DATABASE
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Cart line items (each is an OrderItem).
    /// TODO: GET /api/orders/current → returns { items: [...] }
    /// </summary>
    public IReadOnlyList<OrderItem> Lines { get; set; } = Array.Empty<OrderItem>();

    /// <summary>Cart subtotal before tax.</summary>
    /// <remarks>TODO: comes back from GET /api/orders/current (.subtotal)</remarks>
    public decimal Subtotal { get; set; }

    /// <summary>Tax amount.</summary>
    /// <remarks>TODO: comes back from GET /api/orders/current (.tax)</remarks>
    public decimal Tax { get; set; }

    /// <summary>Final total (subtotal + tax).</summary>
    /// <remarks>TODO: comes back from GET /api/orders/current (.total)</remarks>
    public decimal Total { get; set; }

    // ═══════════════════════════════════════════════════════════════════
    // FORM INPUTS — bound from the checkout form
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Guest's name.
    /// TODO: When auth is live, prefer Cart.UserId over guest fields.
    /// </summary>
    [BindProperty]
    public string? GuestName { get; set; }

    /// <summary>Guest's phone (optional).</summary>
    [BindProperty]
    public string? GuestPhone { get; set; }

    /// <summary>Guest's email (optional).</summary>
    [BindProperty]
    public string? GuestEmail { get; set; }

    /// <summary>
    /// Selected pickup time slot.
    /// TODO: populate from GET /api/pickupslots?date=today and bind the dropdown.
    /// </summary>
    [BindProperty]
    public int? PickupSlotId { get; set; }

    /// <summary>Special instructions for the kitchen (optional).</summary>
    [BindProperty]
    public string? SpecialInstructions { get; set; }

    // ═══════════════════════════════════════════════════════════════════
    // HANDLERS
    // ═══════════════════════════════════════════════════════════════════

    public IActionResult OnGet()
    {
        LoadCart();

        if (!Lines.Any())
        {
            TempData["Message"] = "Your cart is empty. Add items from the menu first.";
            return RedirectToPage("/Menu/Index");
        }

        return Page();
    }

    public IActionResult OnPost()
    {
        LoadCart();

        if (!Lines.Any())
        {
            TempData["Message"] = "Your cart is empty.";
            return RedirectToPage("/Menu/Index");
        }

        if (string.IsNullOrWhiteSpace(GuestName))
        {
            ModelState.AddModelError("", "Please provide a name for the order.");
            return Page();
        }

        // ═══════════════════════════════════════════════════════════════
        // TODO: Real order submission:
        //   1. Set Cart.GuestName/Phone/Email and Cart.PickupSlotId
        //   2. Finalize:
        //        Cart.IsFinalized = true;
        //        Cart.FinalizedAt = DateTime.UtcNow;
        //        Cart.ConfirmationCode = GenerateCode(); // e.g. 6 chars
        //   3. POST /api/orders with the finalized Order
        //   4. CurrentOrder.Clear();
        //   5. Redirect to /Orders/Confirmation?id={newOrderId}
        // ═══════════════════════════════════════════════════════════════

        TempData["Message"] = $"Order placed for {GuestName}.";
        return RedirectToPage("/Orders/Index");
    }

    // ── AJAX handlers (mirror Menu/Index for the inline cart editor) ──
    public IActionResult OnPostUpdateQuantityJson(int menuItemId, int quantity)
    {
        CurrentOrder.UpdateQuantity(menuItemId, quantity);
        return new JsonResult(BuildCartJson());
    }

    public IActionResult OnPostRemoveLineJson(int menuItemId)
    {
        CurrentOrder.RemoveItem(menuItemId);
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
        subtotal = CurrentOrder.Cart.Subtotal,
        tax = CurrentOrder.Cart.Tax,
        total = CurrentOrder.TotalPrice
    };

    private void LoadCart()
    {
        // TODO: Replace with Api call:
        //   var client = _httpClientFactory.CreateClient("Api");
        //   var cart = await client.GetFromJsonAsync<Order>("api/orders/current");
        Lines = CurrentOrder.Lines;
        Subtotal = CurrentOrder.Cart.Subtotal;
        Tax = CurrentOrder.Cart.Tax;
        Total = CurrentOrder.Cart.Total;
    }
}