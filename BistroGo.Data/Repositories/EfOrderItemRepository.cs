using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Data.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace BistroGo.Data.Repositories
{
    public class EfOrderItemRepository : IOrderItemRepository
    {
        private readonly BistroGoDBContext _db;

        public EfOrderItemRepository(BistroGoDBContext db)
        {
            _db = db;
        }

        public async Task<List<OrderItem>> GetAllAsync()
        {
            return await _db.OrderItems
                .AsNoTracking()
                .Include(oi => oi.MenuItem)
                .ToListAsync();
        }

        public async Task<OrderItem?> GetByIdAsync(int id)
        {
            return await _db.OrderItems
                .AsNoTracking()
                .Include(oi => oi.MenuItem)
                .FirstOrDefaultAsync(oi => oi.Id == id);
        }

        public async Task<List<OrderItem>> GetByOrderIdAsync(int orderId)
        {
            return await _db.OrderItems
                .AsNoTracking()
                .Include(oi => oi.MenuItem)
                .Where(oi => oi.OrderId == orderId)
                .ToListAsync();
        }

        public async Task<OrderItem> AddAsync(OrderItem orderItem)
        {
            _db.OrderItems.Add(orderItem);
            await _db.SaveChangesAsync();
            return orderItem;
        }

        public async Task<bool> UpdateAsync(OrderItem orderItem)
        {
            var existing = await _db.OrderItems.FindAsync(orderItem.Id);
            if (existing == null) return false;

            existing.Quantity = orderItem.Quantity;
            existing.UnitPrice = orderItem.UnitPrice;
            existing.SpecialInstructions = orderItem.SpecialInstructions;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _db.OrderItems.FindAsync(id);
            if (existing == null) return false;

            existing.IsArchived = true;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
