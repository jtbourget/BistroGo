using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Web.Cart;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BistroGo.Web.Pages.Orders;

public class CheckoutModel : PageModel
{
    // ═══════════════════════════════════════════════════════════════════
    // DATA TO POPULATE FROM THE API / DATABASE
    // ═══════════════════════════════════════════════════════════════════

    private readonly ICartService _cart;

    public CheckoutModel(ICartService cart)
    {
        _cart = cart;
    }

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

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadCartAsync();

        if (!Lines.Any())
        {
            TempData["Message"] = "Your cart is empty. Add items from the menu first.";
            return RedirectToPage("/Menu/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadCartAsync();

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

    // ── AJAX handler for the modal submission ──
    public IActionResult OnPostPlaceOrderJson(string guestName, string guestPhone, string guestEmail, string specialInstructions)
    {
        LoadCart();
        if (!Lines.Any()) return new JsonResult(new { success = false, message = "Cart is empty." });
        if (string.IsNullOrWhiteSpace(guestName)) return new JsonResult(new { success = false, message = "Name is required." });

        // ═══════════════════════════════════════════════════════════════
        // TODO: POST to /api/orders here. 
        // Set Cart.GuestName = guestName, etc., finalize the order, and save.
        // ═══════════════════════════════════════════════════════════════

        // Clear the cart so it's ready for the next order
        CurrentOrder.Clear();

        return new JsonResult(new { success = true, message = "Order placed successfully!", orderId = 1234 });
    }

    // ── AJAX handlers (mirror Menu/Index for the inline cart editor) ──
    // ── AJAX handlers for the inline cart editor on this page ──
    public async Task<IActionResult> OnPostUpdateQuantityJsonAsync(int menuItemId, int quantity)
    {
        var owner = CartOwnerResolver.Find(HttpContext);
        if (owner != null)
            await _cart.SetQuantityAsync(owner, menuItemId, quantity);

        return new JsonResult(await BuildCartJsonAsync(owner));
    }

    public async Task<IActionResult> OnPostRemoveLineJsonAsync(int menuItemId)
    {
        var owner = CartOwnerResolver.Find(HttpContext);
        if (owner != null)
            await _cart.RemoveItemAsync(owner, menuItemId);

        return new JsonResult(await BuildCartJsonAsync(owner));
    }

    private async Task<object> BuildCartJsonAsync(CartOwner? owner)
    {
        var cart = owner == null ? null : await _cart.GetCartAsync(owner);
        var lines = cart?.Items.OrderBy(i => i.MenuItem!.Name).ToList() ?? new List<OrderItem>();

        return new
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
            subtotal = cart?.Subtotal ?? 0m,
            tax = cart?.Tax ?? 0m,
            total = cart?.Total ?? 0m
        };
    }

    private async Task LoadCartAsync()
    {
        var owner = CartOwnerResolver.Find(HttpContext);
        var cart = owner == null ? null : await _cart.GetCartAsync(owner);

        Lines = cart?.Items.OrderBy(i => i.MenuItem!.Name).ToList() ?? new List<OrderItem>();
        Subtotal = cart?.Subtotal ?? 0m;
        Tax = cart?.Tax ?? 0m;
        Total = cart?.Total ?? 0m;
    }
}