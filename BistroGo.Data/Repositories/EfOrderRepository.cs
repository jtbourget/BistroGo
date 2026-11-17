using BistroGo.Core.Enumerations;
using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Data.DataAccess;
using Microsoft.EntityFrameworkCore;
using BistroGo.Core.Pricing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BistroGo.Data.Repositories
{
    public class EfOrderRepository : IOrderRepository
    {
        private readonly BistroGoDBContext _db;
        private readonly ILogger<EfOrderRepository> _logger;

        public EfOrderRepository(BistroGoDBContext db, ILogger<EfOrderRepository>? logger = null)
        {
            _db = db;
            _logger = logger ?? NullLogger<EfOrderRepository>.Instance;
        }

        public async Task<List<Order>> GetAllAsync()
        {
            _logger.LogDebug("Fetching all active orders");
            return await _db.Orders
                .AsNoTracking()
                .Include(o => o.Status)
                .Include(o => o.PickupSlot)
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.MenuItem)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<Order?> GetByIdAsync(int id)
        {
            _logger.LogDebug("Fetching order with id {OrderId}", id);
            return await _db.Orders
                .Include(o => o.Status)
                .Include(o => o.PickupSlot)
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<List<Order>> GetByUserIdAsync(string userId)
        {
            _logger.LogDebug("Fetching orders for user {UserId}", userId);
            return await _db.Orders
                .AsNoTracking()
                .Include(o => o.Status)
                .Include(o => o.PickupSlot)
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.MenuItem)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<Order> AddAsync(Order order)
        {
            _logger.LogInformation("Adding new order for user {UserId}, Guest: {GuestName}", order.UserId, order.GuestName);
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Added order with id {OrderId}", order.Id);
            return order;
        }

        public async Task<bool> UpdateAsync(Order order)
        {
            _logger.LogInformation("Updating order {OrderId}", order.Id);
            var existing = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            if (existing == null)
            {
                _logger.LogWarning("Cannot update order {OrderId}: not found", order.Id);
                return false;
            }

            existing.StatusId = order.StatusId;
            existing.PickupSlotId = order.PickupSlotId;
            existing.GuestName = order.GuestName;
            existing.GuestEmail = order.GuestEmail;
            existing.GuestPhone = order.GuestPhone;
            existing.ConfirmationCode = order.ConfirmationCode;
            existing.IsFinalized = order.IsFinalized;
            existing.FinalizedAt = order.FinalizedAt;

            if (order.Items != null && order.Items.Any())
            {
                foreach (var item in order.Items)
                {
                    var existingItem = existing.Items.FirstOrDefault(i => (item.Id != 0 && i.Id == item.Id) || (item.MenuItemId != 0 && i.MenuItemId == item.MenuItemId));
                    if (existingItem != null)
                    {
                        existingItem.SpecialInstructions = item.SpecialInstructions;
                        existingItem.Quantity = item.Quantity;
                        existingItem.UnitPrice = item.UnitPrice;
                    }
                }
            }

            if (order.DiscountAmount > 0)
            {
                existing.DiscountAmount = order.DiscountAmount;
                var discountedSubtotal = Math.Round(existing.Subtotal - existing.DiscountAmount, 2);
                existing.Tax = Math.Round(discountedSubtotal * PricingConstants.TaxRate, 2);
                existing.Total = Math.Round(discountedSubtotal + existing.Tax, 2);
            }
            else
            {
                OrderPricing.Recalculate(existing);
            }
            await _db.SaveChangesAsync();
            _logger.LogInformation("Successfully updated order {OrderId}", order.Id);
            return true;
        }

        public async Task<SlotSaveResult> UpdateIfSlotHasRoomAsync(Order order, int maxOrders)
        {
            _logger.LogInformation("Beginning transactional slot capacity check and order update for order {OrderId}, slot {SlotId}",
                order.Id, order.PickupSlotId);

            // Starting the transaction takes SQLite's write lock, so a second customer
            // placing an order waits here until this one commits or rolls back.
            await using var tx = await _db.Database.BeginTransactionAsync();

            if (!await UpdateAsync(order))
            {
                _logger.LogWarning("Transactional update failed: order {OrderId} not found", order.Id);
                return SlotSaveResult.OrderNotFound;   // leaving the method rolls back
            }

            // This count already includes the order we just saved.
            var booked = await _db.BookedOrders(order.PickupSlotId!.Value).CountAsync();
            if (booked > maxOrders)
            {
                _logger.LogWarning("Transactional update rolled back: slot {SlotId} is over capacity ({Booked}/{MaxOrders})",
                    order.PickupSlotId.Value, booked, maxOrders);
                await tx.RollbackAsync();
                _db.ChangeTracker.Clear();   // forget the in-memory "finalized" copy
                return SlotSaveResult.SlotFull;
            }

            await tx.CommitAsync();
            _logger.LogInformation("Transactional update committed for order {OrderId}, slot {SlotId} has {Booked}/{MaxOrders} bookings",
                order.Id, order.PickupSlotId.Value, booked, maxOrders);
            return SlotSaveResult.Saved;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            _logger.LogInformation("Soft-deleting order {OrderId}", id);
            var existing = await _db.Orders.FindAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("Cannot delete order {OrderId}: not found", id);
                return false;
            }

            // Soft delete
            existing.IsArchived = true;
            existing.ArchivedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            _logger.LogInformation("Successfully soft-deleted order {OrderId}", id);
            return true;
        }

        public async Task<List<OrderItem>> GetLinesByOrderIdAsync(int orderId)
        {
            _logger.LogDebug("Fetching lines for order {OrderId}", orderId);
            return await _db.OrderItems
                .AsNoTracking()
                .Include(oi => oi.MenuItem)
                .Where(oi => oi.OrderId == orderId)
                .ToListAsync();
        }

        public async Task<OrderItem?> GetLineByIdAsync(int id)
        {
            _logger.LogDebug("Fetching line item with id {LineItemId}", id);
            return await _db.OrderItems
                .AsNoTracking()
                .Include(oi => oi.MenuItem)
                .FirstOrDefaultAsync(oi => oi.Id == id);
        }

        public async Task<OrderItem> AddLineAsync(OrderItem line)
        {
            _logger.LogInformation("Adding line item to order {OrderId}", line.OrderId);
            _db.OrderItems.Add(line);
            await _db.SaveChangesAsync();

            var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == line.OrderId);
            if (order != null)
            {
                OrderPricing.Recalculate(order);
                await _db.SaveChangesAsync();
            }

            _logger.LogInformation("Added line item {LineItemId} to order {OrderId}", line.Id, line.OrderId);
            return line;
        }

        public async Task<bool> UpdateLineAsync(OrderItem line)
        {
            _logger.LogInformation("Updating line item {LineItemId}", line.Id);
            var existing = await _db.OrderItems.FindAsync(line.Id);
            if (existing == null)
            {
                _logger.LogWarning("Cannot update line item {LineItemId}: not found", line.Id);
                return false;
            }

            existing.Quantity = line.Quantity;
            existing.UnitPrice = line.UnitPrice;
            existing.SpecialInstructions = line.SpecialInstructions;

            await _db.SaveChangesAsync();

            var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == existing.OrderId);
            if (order != null)
            {
                OrderPricing.Recalculate(order);
                await _db.SaveChangesAsync();
            }

            _logger.LogInformation("Successfully updated line item {LineItemId}", line.Id);
            return true;
        }

        public async Task<bool> DeleteLineAsync(int id)
        {
            _logger.LogInformation("Soft-deleting line item {LineItemId}", id);
            var line = await _db.OrderItems.FindAsync(id);
            if (line == null)
            {
                _logger.LogWarning("Cannot delete line item {LineItemId}: not found", id);
                return false;
            }

            line.IsArchived = true;
            await _db.SaveChangesAsync();

            var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == line.OrderId);
            if (order != null)
            {
                OrderPricing.Recalculate(order);
                await _db.SaveChangesAsync();
            }

            _logger.LogInformation("Successfully soft-deleted line item {LineItemId}", id);
            return true;
        }
    }
}
