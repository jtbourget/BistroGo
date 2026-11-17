using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Core.Pricing;
using BistroGo.Data.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BistroGo.Data.Services
{
    public class EfCartService : ICartService
    {
        private const int MaxPerLine = 99;
        private readonly BistroGoDBContext _db;
        private readonly ILogger<EfCartService> _logger;

        public EfCartService(BistroGoDBContext db, ILogger<EfCartService>? logger = null)
        {
            _db = db;
            _logger = logger ?? NullLogger<EfCartService>.Instance;
        }

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

        public async Task<Order?> GetCartAsync(CartOwner owner)
        {
            _logger.LogDebug("Retrieving open cart for owner (UserId: {UserId}, GuestToken: {GuestToken})",
                owner.UserId, owner.GuestToken);

            return await OpenCarts(owner).AsNoTracking()
                .Include(or => or.Items).ThenInclude(i => i.MenuItem)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> AddItemAsync(CartOwner owner, int menuItemId, int quantity = 1)
        {
            if (quantity < 1) throw new ArgumentOutOfRangeException(nameof(quantity));

            _logger.LogInformation("Adding MenuItemId {MenuItemId} (Quantity: {Quantity}) to cart for owner (UserId: {UserId}, GuestToken: {GuestToken})",
                menuItemId, quantity, owner.UserId, owner.GuestToken);

            var menuItem = await _db.MenuItems.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == menuItemId && m.IsAvailable);
            if (menuItem == null)
            {
                _logger.LogWarning("Cannot add item {MenuItemId}: item not found or unavailable", menuItemId);
                return false;
            }

            var cart = await OpenCarts(owner).Include(or => or.Items).FirstOrDefaultAsync();
            if (cart == null)
            {
                _logger.LogInformation("Creating new cart for owner (UserId: {UserId}, GuestToken: {GuestToken})",
                    owner.UserId, owner.GuestToken);
                cart = new Order { UserId = owner.UserId, GuestToken = owner.GuestToken };
                _db.Orders.Add(cart);
            }

            var line = cart.Items.FirstOrDefault(i => i.MenuItemId == menuItemId);
            if (line == null)
            {
                cart.Items.Add(new OrderItem
                {
                    MenuItemId = menuItemId,
                    Quantity = Math.Min(quantity, MaxPerLine),
                    UnitPrice = menuItem.Price          // price snapshot
                });
            }
            else
            {
                line.Quantity = Math.Min(line.Quantity + quantity, MaxPerLine);
            }

            OrderPricing.Recalculate(cart);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Added MenuItemId {MenuItemId} to cart {CartId}. New total: {Total}",
                menuItemId, cart.Id, cart.Total);
            return true;
        }

        public async Task<bool> SetQuantityAsync(CartOwner owner, int menuItemId, int quantity)
        {
            _logger.LogInformation("Setting quantity of item {MenuItemId} to {Quantity} for owner (UserId: {UserId}, GuestToken: {GuestToken})",
                menuItemId, quantity, owner.UserId, owner.GuestToken);

            var cart = await OpenCarts(owner).Include(or => or.Items).FirstOrDefaultAsync();
            var line = cart?.Items.FirstOrDefault(i => i.MenuItemId == menuItemId);
            if (cart == null || line == null)
            {
                _logger.LogWarning("Cannot set quantity: cart or item {MenuItemId} not found", menuItemId);
                return false;
            }

            if (quantity < 1)
            {
                _logger.LogInformation("Removing line item {LineId} for item {MenuItemId} from cart {CartId}",
                    line.Id, menuItemId, cart.Id);
                cart.Items.Remove(line);
                _db.OrderItems.Remove(line);
            }
            else
            {
                line.Quantity = Math.Min(quantity, MaxPerLine);
            }

            OrderPricing.Recalculate(cart);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Updated cart {CartId}. New total: {Total}", cart.Id, cart.Total);
            return true;
        }

        public Task<bool> RemoveItemAsync(CartOwner owner, int menuItemId) =>
            SetQuantityAsync(owner, menuItemId, 0);

        public async Task MergeGuestCartAsync(Guid guestToken, string userId)
        {
            _logger.LogInformation("Merging guest cart {GuestToken} into user cart for user {UserId}", guestToken, userId);

            var guestCart = await _db.Orders.Include(or => or.Items)
                .FirstOrDefaultAsync(or => !or.IsFinalized && or.GuestToken == guestToken);
            if (guestCart == null)
            {
                _logger.LogInformation("No guest cart found for token {GuestToken} to merge", guestToken);
                return;
            }

            var userCart = await _db.Orders.Include(or => or.Items)
                .FirstOrDefaultAsync(or => !or.IsFinalized && or.UserId == userId);

            if (userCart == null)
            {
                // No account cart yet: the guest cart simply becomes the user's cart.
                _logger.LogInformation("Promoting guest cart {GuestToken} to user cart for {UserId}", guestToken, userId);
                guestCart.UserId = userId;
                guestCart.GuestToken = null;
            }
            else
            {
                _logger.LogInformation("Folding guest cart {GuestToken} items into user cart {UserCartId}", guestToken, userCart.Id);
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
            _logger.LogInformation("Successfully merged guest cart for token {GuestToken} into user {UserId}", guestToken, userId);
        }
    }
}
