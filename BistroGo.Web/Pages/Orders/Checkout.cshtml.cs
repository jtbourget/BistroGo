using BistroGo.Core.Enumerations;
using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Core.Pricing;
using BistroGo.Data.Identity;
using BistroGo.Web.Cart;
using BistroGo.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BistroGo.Web.Pages.Orders;

public class CheckoutModel : PageModel
{
    private readonly ICartService _cart;
    private readonly IOrderRepository _orderRepo;
    private readonly IOrderService _orderService;
    private readonly IPickupTimeSlotRepository _pickupRepo;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITaxRateApiClient _taxRateApi;

    
    public CheckoutModel(
        ICartService cart,
        IOrderRepository orderRepo,
        IOrderService orderService,
        IPickupTimeSlotRepository pickupRepo,
        ITaxRateApiClient taxRateApi,
        UserManager<ApplicationUser> userManager)
    {
        _cart = cart;
        _orderRepo = orderRepo;
        _orderService = orderService;
        _pickupRepo = pickupRepo;
        _taxRateApi = taxRateApi;
        _userManager = userManager;
    }

    /// <summary>Cart line items (each is an OrderItem).</summary>
    public IReadOnlyList<OrderItem> Lines { get; set; } = Array.Empty<OrderItem>();

    /// <summary>Cart subtotal before discounts and tax.</summary>
    public decimal Subtotal { get; set; }

    /// <summary>The dollar amount of employee discount (if applicable).</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>Tax amount calculated on the discounted subtotal.</summary>
    public decimal Tax { get; set; }

    /// <summary>Final total (subtotal + tax).</summary>
    public decimal Total { get; set; }

    /// <summary>List of active pickup time slots for selection.</summary>
    public List<PickupTimeSlot> AvailableSlots { get; set; } = new();

    // ═══════════════════════════════════════════════════════════════════
    // FORM INPUTS — bound from the checkout form
    // ═══════════════════════════════════════════════════════════════════

    [BindProperty]
    public string? GuestName { get; set; }

    [BindProperty]
    public string? GuestPhone { get; set; }

    [BindProperty]
    public string? GuestEmail { get; set; }

    [BindProperty]
    public int? PickupSlotId { get; set; }

    [BindProperty]
    public string? SpecialInstructions { get; set; }

    // ═══════════════════════════════════════════════════════════════════
    // HANDLERS
    // ═══════════════════════════════════════════════════════════════════

    // ── GET ──────────────────────────────────────────────────────────
    public async Task<IActionResult> OnGetAsync()
    {
        await LoadCartAsync();

        if (!Lines.Any())
        {
            TempData["Message"] = "Your cart is empty. Add items from the menu first.";
            return RedirectToPage("/Menu/Index");
        }

        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                GuestName ??= $"{user.FirstName} {user.LastName}".Trim();
                GuestEmail ??= user.Email;
                GuestPhone ??= user.PhoneNumber;
            }
        }

        var slots = await _pickupRepo.GetAllAsync();
        AvailableSlots = slots.Where(s => !s.IsArchived).ToList();
        if (AvailableSlots.Any() && PickupSlotId == null)
        {
            PickupSlotId = AvailableSlots.First().Id;
        }

        return Page();
    }

    // ── POST (Standard Form) ─────────────────────────────────────────
    public async Task<IActionResult> OnPostAsync()
    {
        await LoadCartAsync();

        if (!Lines.Any())
        {
            TempData["Message"] = "Your cart is empty.";
            return RedirectToPage("/Menu/Index");
        }

        var owner = CartOwnerResolver.Find(HttpContext);
        if (owner == null)
        {
            ModelState.AddModelError("", "No active cart session.");
            return Page();
        }

        var cart = await _cart.GetCartAsync(owner);
        if (cart == null || !cart.Items.Any())
        {
            ModelState.AddModelError("", "Your cart is empty.");
            return Page();
        }

        if (cart.IsFinalized)
        {
            ModelState.AddModelError("", "This order has already been placed.");
            return Page();
        }

        string? resolvedUserId = null;
        string finalCustomerName;
        string? finalPhone = !string.IsNullOrWhiteSpace(GuestPhone) ? GuestPhone.Trim() : null;
        string? finalEmail = !string.IsNullOrWhiteSpace(GuestEmail) ? GuestEmail.Trim() : null;

        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            resolvedUserId = _userManager.GetUserId(User) ?? user?.Id;
            var defaultName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : "Customer";
            finalCustomerName = !string.IsNullOrWhiteSpace(GuestName) ? GuestName.Trim() : defaultName;
            finalEmail ??= user?.Email;
            finalPhone ??= user?.PhoneNumber;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(GuestName))
            {
                ModelState.AddModelError("GuestName", "Please provide a name for the order.");
                var slots = await _pickupRepo.GetAllAsync();
                AvailableSlots = slots.Where(s => !s.IsArchived).ToList();
                return Page();
            }

            finalCustomerName = GuestName.Trim();
        }

        int? resolvedSlotId = PickupSlotId ?? cart.PickupSlotId;
        if (resolvedSlotId == null)
        {
            var availableSlot = (await _pickupRepo.GetAllAsync()).FirstOrDefault(s => !s.IsArchived);
            resolvedSlotId = availableSlot?.Id ?? 1;
        }

        cart.UserId = resolvedUserId;
        cart.GuestName = finalCustomerName;
        cart.GuestPhone = finalPhone;
        cart.GuestEmail = finalEmail;
        cart.PickupSlotId = resolvedSlotId;

        if (!string.IsNullOrWhiteSpace(SpecialInstructions))
        {
            var note = SpecialInstructions.Trim();
            foreach (var item in cart.Items)
            {
                if (string.IsNullOrWhiteSpace(item.SpecialInstructions))
                {
                    item.SpecialInstructions = note;
                }
            }
        }

        var updated = await _orderRepo.UpdateAsync(cart);
        if (!updated)
        {
            ModelState.AddModelError("", "Failed to save order details.");
            return Page();
        }

        var result = await _orderService.PlaceOrderAsync(cart.Id);
        if (result == PlaceOrderResult.Placed)
        {
            if (owner.GuestToken != null)
            {
                CartOwnerResolver.ClearGuestToken(HttpContext);
            }

            TempData["Message"] = $"Order #{cart.Id} placed successfully for {finalCustomerName}.";

            if (User.IsInRole(Roles.Manager) || User.IsInRole(Roles.KitchenStaff))
            {
                return RedirectToPage("/Orders/Index");
            }
            return RedirectToPage("/Menu/Index");
        }

        string errorMsg = result switch
        {
            PlaceOrderResult.AlreadyPlaced => "This order has already been placed.",
            PlaceOrderResult.NotFound => "Order was not found.",
            PlaceOrderResult.PickupSlotRequired => "A pickup time slot is required.",
            _ => "Failed to place order."
        };

        ModelState.AddModelError("", errorMsg);
        return Page();
    }

    // ── AJAX handler for the modal submission ──
    public async Task<IActionResult> OnPostPlaceOrderJsonAsync(
        string guestName,
        string? guestPhone,
        string? guestEmail,
        string? specialInstructions,
        int? pickupSlotId)
    {
        var owner = CartOwnerResolver.Find(HttpContext);
        if (owner == null)
            return new JsonResult(new { success = false, message = "No active cart session." });

        var cart = await _cart.GetCartAsync(owner);
        if (cart == null || !cart.Items.Any())
            return new JsonResult(new { success = false, message = "Cart is empty." });

        if (cart.IsFinalized)
            return new JsonResult(new { success = false, message = "This order has already been placed." });

        string? resolvedUserId = null;
        string finalCustomerName;
        string? finalPhone = !string.IsNullOrWhiteSpace(guestPhone) ? guestPhone.Trim() : null;
        string? finalEmail = !string.IsNullOrWhiteSpace(guestEmail) ? guestEmail.Trim() : null;

        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            resolvedUserId = _userManager.GetUserId(User) ?? user?.Id;
            var defaultName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : "Customer";
            finalCustomerName = !string.IsNullOrWhiteSpace(guestName) ? guestName.Trim() : defaultName;
            finalEmail ??= user?.Email;
            finalPhone ??= user?.PhoneNumber;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(guestName))
                return new JsonResult(new { success = false, message = "Name is required." });

            finalCustomerName = guestName.Trim();
        }

        int? resolvedSlotId = pickupSlotId ?? PickupSlotId ?? cart.PickupSlotId;
        if (resolvedSlotId == null)
        {
            var availableSlot = (await _pickupRepo.GetAllAsync()).FirstOrDefault(s => !s.IsArchived);
            resolvedSlotId = availableSlot?.Id ?? 1;
        }

        cart.UserId = resolvedUserId;
        cart.GuestName = finalCustomerName;
        cart.GuestPhone = finalPhone;
        cart.GuestEmail = finalEmail;
        cart.PickupSlotId = resolvedSlotId;

        if (!string.IsNullOrWhiteSpace(specialInstructions))
        {
            var note = specialInstructions.Trim();
            foreach (var item in cart.Items)
            {
                if (string.IsNullOrWhiteSpace(item.SpecialInstructions))
                {
                    item.SpecialInstructions = note;
                }
            }
        }

        var updated = await _orderRepo.UpdateAsync(cart);
        if (!updated)
        {
            return new JsonResult(new { success = false, message = "Failed to update order details." });
        }

        var result = await _orderService.PlaceOrderAsync(cart.Id);
        if (result == PlaceOrderResult.Placed)
        {
            if (owner.GuestToken != null)
            {
                CartOwnerResolver.ClearGuestToken(HttpContext);
            }

            return new JsonResult(new
            {
                success = true,
                message = "Order placed successfully!",
                orderId = cart.Id,
                confirmationCode = cart.ConfirmationCode
            });
        }

        string errorMsg = result switch
        {
            PlaceOrderResult.AlreadyPlaced => "Order has already been placed.",
            PlaceOrderResult.NotFound => "Order was not found.",
            PlaceOrderResult.PickupSlotRequired => "A pickup time slot is required.",
            _ => "Failed to place order."
        };

        return new JsonResult(new { success = false, message = errorMsg });
    }

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

        if (cart != null)
        {
            // Fetch the current tax rate from the API (authoritative source)
            // Falls back to the in-memory service if API call fails
            var currentTaxRate = await GetCurrentTaxRateAsync();

            // Get the current user's roles to determine if they qualify for employee discount
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                // Recalculate the cart with discount consideration and current tax rate
                OrderPricing.Recalculate(cart, userRoles, currentTaxRate);
            }
            else
            {
                // For guests, recalculate without discount but with current tax rate
                OrderPricing.Recalculate(cart, userRoles: null, currentTaxRate);
            }

            Subtotal = cart.Subtotal;
            DiscountAmount = cart.DiscountAmount;
            Tax = cart.Tax;
            Total = cart.Total;
        }
    }

    /// <summary>
    /// Fetches the current tax rate from the API endpoint using the TaxRateApiClient.
    /// Falls back to the in-memory service if the API call fails.
    /// </summary>
    private async Task<decimal?> GetCurrentTaxRateAsync()
    {
        try
        {
            // Fetch from the service (authoritative source)
            var rate = await _taxRateApi.GetCurrentRateAsync();
            Console.WriteLine($"Fetched tax rate from service: {rate}");
            return rate;
        }
        catch (Exception ex)
        {
            // Log error and fall back to service
            Console.WriteLine($"Error fetching tax rate from service: {ex.Message}. Using in-memory service fallback.");
            return null; // Will fall back to default in Recalculate
        }
    }
}
