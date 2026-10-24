using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Core.Pricing;
using BistroGo.Data.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace BistroGo.Data.Services
{
    public class EfCartService : ICartService
    {
        private const int MaxPerLine = 99;
        private readonly BistroGoDBContext _db;

        public EfCartService(BistroGoDBContext db) { _db = db; }

        // EVERY method starts here, so a caller can only ever reach their own open cart.
        private IQueryable<Order> OpenCarts(CartOwner owner)
        {
            var carts = _db.Orders.Where(or => !or.IsFinalized);

            if (owner.UserId != null)
                return carts.Where(or => or.UserId == owner.UserId);
            if (owner.GuestToken != null)
                return carts.Where(or => or.GuestToken == owner.GuestToken);

            // An owner with neither would otherwise match "GuestToken IS NULL" = every user's cart.
            throw new ArgumentException("Cart owner needs a UserId or a GuestToken.", nameof(owner));
        }

        public async Task<Order?> GetCartAsync(CartOwner owner) =>
            await OpenCarts(owner).AsNoTracking()
                .Include(or => or.Items).ThenInclude(i => i.MenuItem)
                .FirstOrDefaultAsync();

        public async Task<bool> AddItemAsync(CartOwner owner, int menuItemId, int quantity = 1)
        {
            if (quantity < 1) throw new ArgumentOutOfRangeException(nameof(quantity));

            var menuItem = await _db.MenuItems.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == menuItemId && m.IsAvailable);
            if (menuItem == null) return false;

            var cart = await OpenCarts(owner).Include(or => or.Items).FirstOrDefaultAsync();
            if (cart == null)
            {
                cart = new Order { UserId = owner.UserId, GuestToken = owner.GuestToken };
                _db.Orders.Add(cart);
            }

            var line = cart.Items.FirstOrDefault(i => i.MenuItemId == menuItemId);
            if (line == null)
                cart.Items.Add(new OrderItem
                {
                    MenuItemId = menuItemId,
                    Quantity = Math.Min(quantity, MaxPerLine),
                    UnitPrice = menuItem.Price          // price snapshot
                });
            else
                line.Quantity = Math.Min(line.Quantity + quantity, MaxPerLine);

            OrderPricing.Recalculate(cart);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetQuantityAsync(CartOwner owner, int menuItemId, int quantity)
        {
            var cart = await OpenCarts(owner).Include(or => or.Items).FirstOrDefaultAsync();
            var line = cart?.Items.FirstOrDefault(i => i.MenuItemId == menuItemId);
            if (cart == null || line == null) return false;

            if (quantity < 1)
            {
                cart.Items.Remove(line);
                _db.OrderItems.Remove(line);
            }
            else
            {
                line.Quantity = Math.Min(quantity, MaxPerLine);
            }

            OrderPricing.Recalculate(cart);
            await _db.SaveChangesAsync();
            return true;
        }

        public Task<bool> RemoveItemAsync(CartOwner owner, int menuItemId) =>
            SetQuantityAsync(owner, menuItemId, 0);

        public async Task MergeGuestCartAsync(Guid guestToken, string userId)
        {
            var guestCart = await _db.Orders.Include(or => or.Items)
                .FirstOrDefaultAsync(or => !or.IsFinalized && or.GuestToken == guestToken);
            if (guestCart == null) return;

            var userCart = await _db.Orders.Include(or => or.Items)
                .FirstOrDefaultAsync(or => !or.IsFinalized && or.UserId == userId);

            if (userCart == null)
            {
                // No account cart yet: the guest cart simply becomes the user's cart.
                guestCart.UserId = userId;
                guestCart.GuestToken = null;
            }
            else
            {
                foreach (var gl in guestCart.Items)
                {
                    var existing = userCart.Items.FirstOrDefault(i => i.MenuItemId == gl.MenuItemId);
                    if (existing == null)
                        userCart.Items.Add(new OrderItem
                        {
                            MenuItemId = gl.MenuItemId,
                            Quantity = gl.Quantity,
                            UnitPrice = gl.UnitPrice,
                            SpecialInstructions = gl.SpecialInstructions
                        });
                    else
                        existing.Quantity = Math.Min(existing.Quantity + gl.Quantity, MaxPerLine);
                }
                OrderPricing.Recalculate(userCart);
                _db.Orders.Remove(guestCart);       // cascade deletes its lines
            }

            await _db.SaveChangesAsync();           // one transaction: all of it or none of it
        }
    }
}