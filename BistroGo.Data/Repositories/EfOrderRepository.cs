using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Data.DataAccess;
using Microsoft.EntityFrameworkCore;
using BistroGo.Core.Pricing;

namespace BistroGo.Data.Repositories
{
    public class EfOrderRepository : IOrderRepository
    {
        private readonly BistroGoDBContext _db;

        public EfOrderRepository(BistroGoDBContext db)
        {
            _db = db;
        }

        public async Task<List<Order>> GetAllAsync()
        {
            return await _db.Orders
                .AsNoTracking()
                .Include(o => o.Status)
                .Include(o => o.PickupSlot)
                .Include(o => o.Items)
                    .ThenInclude(i => i.MenuItem)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<Order?> GetByIdAsync(int id)
        {
            return await _db.Orders
                .AsNoTracking()
                .Include(o => o.Status)
                .Include(o => o.PickupSlot)
                .Include(o => o.Items)
                    .ThenInclude(i => i.MenuItem)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<List<Order>> GetByUserIdAsync(string userId)
        {
            return await _db.Orders
                .AsNoTracking()
                .Include(o => o.Status)
                .Include(o => o.PickupSlot)
                .Include(o => o.Items)
                    .ThenInclude(i => i.MenuItem)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<Order> AddAsync(Order order)
        {
            OrderPricing.Recalculate(order);
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();
            return order;
        }

        public async Task<bool> UpdateAsync(Order order)
        {
            var existing = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            if (existing == null) return false;

            existing.GuestName = order.GuestName;
            existing.GuestPhone = order.GuestPhone;
            existing.GuestEmail = order.GuestEmail;
            existing.PickupSlotId = order.PickupSlotId;
            existing.StatusId = order.StatusId;
            existing.IsFinalized = order.IsFinalized;
            existing.FinalizedAt = order.FinalizedAt;
            existing.ConfirmationCode = order.ConfirmationCode;

            OrderPricing.Recalculate(existing);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<SlotSaveResult> UpdateIfSlotHasRoomAsync(Order order, int maxOrders)
        {
            // Starting the transaction takes SQLite's write lock, so a second customer
            // placing an order waits here until this one commits or rolls back.
            await using var tx = await _db.Database.BeginTransactionAsync();

            if (!await UpdateAsync(order))
            {
                return SlotSaveResult.OrderNotFound;   // leaving the method rolls back
            }

            // This count already includes the order we just saved.
            var booked = await _db.BookedOrders(order.PickupSlotId!.Value).CountAsync();
            if (booked > maxOrders)
            {
                await tx.RollbackAsync();
                _db.ChangeTracker.Clear();   // forget the in-memory "finalized" copy
                return SlotSaveResult.SlotFull;
            }

            await tx.CommitAsync();
            return SlotSaveResult.Saved;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _db.Orders.FindAsync(id);
            if (existing == null) return false;

            // Soft delete
            existing.IsArchived = true;
            existing.ArchivedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<OrderItem>> GetLinesByOrderIdAsync(int orderId)
        {
            return await _db.OrderItems
                .AsNoTracking()
                .Include(oi => oi.MenuItem)
                .Where(oi => oi.OrderId == orderId)
                .ToListAsync();
        }

        public async Task<OrderItem?> GetLineByIdAsync(int id)
        {
            return await _db.OrderItems
                .AsNoTracking()
                .Include(oi => oi.MenuItem)
                .FirstOrDefaultAsync(oi => oi.Id == id);
        }

        public async Task<OrderItem> AddLineAsync(OrderItem line)
        {
            _db.OrderItems.Add(line);
            await _db.SaveChangesAsync();

            var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == line.OrderId);
            if (order != null)
            {
                OrderPricing.Recalculate(order);
                await _db.SaveChangesAsync();
            }

            return line;
        }

        public async Task<bool> UpdateLineAsync(OrderItem line)
        {
            var existing = await _db.OrderItems.FindAsync(line.Id);
            if (existing == null) return false;

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

            return true;
        }

        public async Task<bool> DeleteLineAsync(int id)
        {
            var line = await _db.OrderItems.FindAsync(id);
            if (line == null) return false;

            line.IsArchived = true;
            await _db.SaveChangesAsync();

            var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == line.OrderId);
            if (order != null)
            {
                OrderPricing.Recalculate(order);
                await _db.SaveChangesAsync();
            }

            return true;
        }
    }
}
