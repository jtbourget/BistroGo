using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Data.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace BistroGo.Data.Repositories
{
    public class EfOrderRepository : IOrderRepository
    {
        private readonly BistroGoDBContext _db;
        private const decimal TaxRate = 0.08m;

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
            RecalculateTotals(order);
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

            RecalculateTotals(existing);
            await _db.SaveChangesAsync();
            return true;
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

        public async Task<bool> PlaceOrderAsync(int id)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null || order.IsFinalized) return false;

            order.IsFinalized = true;
            order.FinalizedAt = DateTime.UtcNow;
            order.StatusId = OrderStatusIds.Received;
            order.ConfirmationCode ??= GenerateConfirmationCode();
            RecalculateTotals(order);

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
                RecalculateTotals(order);
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
                RecalculateTotals(order);
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
                RecalculateTotals(order);
                await _db.SaveChangesAsync();
            }

            return true;
        }

        private static void RecalculateTotals(Order order)
        {
            if (order.Items != null && order.Items.Any())
            {
                order.Subtotal = Math.Round(order.Items.Where(i => !i.IsArchived).Sum(i => i.Quantity * i.UnitPrice), 2);
                order.Tax = Math.Round(order.Subtotal * TaxRate, 2);
                order.Total = order.Subtotal + order.Tax;
            }
        }

        private static string GenerateConfirmationCode()
        {
            return "BG-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        }
    }
}
