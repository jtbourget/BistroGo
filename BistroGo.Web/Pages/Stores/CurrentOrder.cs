using BistroGo.Core.Models;

namespace BistroGo.Web.Stores;

/// <summary>
/// Holds the in-progress (unfinalized) order — the "cart".
/// When checkout completes, this Order gets finalized and saved to the DB.
/// 
/// LATER: Replace with an Api-backed cart:
///   - AddItem        → POST   /api/orders/current/items
///   - UpdateQuantity → PUT    /api/orders/current/items/{menuItemId}
///   - RemoveItem     → DELETE /api/orders/current/items/{menuItemId}
///   - Cart/Lines     → GET    /api/orders/current
/// </summary>
public static class CurrentOrder
{
    public static Order Cart { get; private set; } = NewEmptyCart();

    public static IReadOnlyList<OrderItem> Lines => Cart.Items.ToList();
    public static int TotalItems => Cart.Items.Sum(i => i.Quantity);
    public static decimal TotalPrice => Cart.Items.Sum(i => i.UnitPrice * i.Quantity);
    public static bool IsEmpty => Cart.Items.Count == 0;

    public static void AddItem(MenuItem menuItem)
    {
        var existing = Cart.Items.FirstOrDefault(i => i.MenuItemId == menuItem.Id);
        if (existing != null)
        {
            existing.Quantity++;
        }
        else
        {
            Cart.Items.Add(new OrderItem
            {
                MenuItemId = menuItem.Id,
                MenuItem = menuItem,
                UnitPrice = menuItem.Price,
                Quantity = 1
            });
        }
        RecalculateTotals();
    }

    public static void UpdateQuantity(int menuItemId, int quantity)
    {
        var line = Cart.Items.FirstOrDefault(i => i.MenuItemId == menuItemId);
        if (line == null) return;

        if (quantity <= 0)
        {
            Cart.Items.Remove(line);
        }
        else
        {
            line.Quantity = quantity;
        }

        RecalculateTotals();
    }

    public static void RemoveItem(int menuItemId)
    {
        // ICollection<T> has no RemoveAll — copy matches first, then remove
        var toRemove = Cart.Items.Where(i => i.MenuItemId == menuItemId).ToList();
        foreach (var item in toRemove)
        {
            Cart.Items.Remove(item);
        }
        RecalculateTotals();
    }

    public static void Clear()
    {
        Cart = NewEmptyCart();
    }

    private static Order NewEmptyCart() => new()
    {
        IsFinalized = false,
        CreatedAt = DateTime.UtcNow,
        Items = new List<OrderItem>()
    };

    private static void RecalculateTotals()
    {
        Cart.Subtotal = Cart.Items.Sum(i => i.UnitPrice * i.Quantity);

        // TODO: real tax rate from configuration
        Cart.Tax = Math.Round(Cart.Subtotal * 0.08m, 2);

        Cart.Total = Cart.Subtotal + Cart.Tax;
    }
}