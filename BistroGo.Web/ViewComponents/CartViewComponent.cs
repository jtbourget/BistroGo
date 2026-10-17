using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Web.Cart;
using Microsoft.AspNetCore.Mvc;

namespace BistroGo.Web.ViewComponents;

/// <summary>
/// Holds the data as a bundle needed to render a cart view component, either the badge or the modal.
/// </summary>
/// <param name="Lines">The list of order items in the cart.</param>
/// <param name="Count">The total number of items in the cart.</param>
/// <param name="Total">The total cost of the items in the cart.</param>
public record CartViewModel(List<OrderItem> Lines, int Count, decimal Total);

public class CartViewComponent : ViewComponent
{
    private readonly ICartService _cart;

    public CartViewComponent(ICartService cart)
    {
        _cart = cart;
    }

    // part = "Badge" or "Modal": which view to render
    public async Task<IViewComponentResult> InvokeAsync(string part)
    {
        // Find (not FindOrCreate): just looking at a page must never create a cookie or a cart.
        var owner = CartOwnerResolver.Find(HttpContext);
        var cart = owner == null ? null : await _cart.GetCartAsync(owner);

        var lines = cart?.Items.OrderBy(i => i.MenuItem!.Name).ToList() ?? new List<OrderItem>();
        var model = new CartViewModel(lines, lines.Sum(i => i.Quantity), cart?.Total ?? 0m);

        return View(part, model);
    }
}