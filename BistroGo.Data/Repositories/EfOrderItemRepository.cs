using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Data.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BistroGo.Data.Repositories
{
    public class EfOrderItemRepository : IOrderItemRepository
    {
        private readonly BistroGoDBContext _db;
        private readonly ILogger<EfOrderItemRepository> _logger;

        public EfOrderItemRepository(BistroGoDBContext db, ILogger<EfOrderItemRepository>? logger = null)
        {
            _db = db;
            _logger = logger ?? NullLogger<EfOrderItemRepository>.Instance;
        }

        public async Task<List<OrderItem>> GetAllAsync()
        {
            _logger.LogDebug("Fetching all order items");
            return await _db.OrderItems
                .AsNoTracking()
                .Include(oi => oi.MenuItem)
                .ToListAsync();
        }

        public async Task<OrderItem?> GetByIdAsync(int id)
        {
            _logger.LogDebug("Fetching order item with id {OrderItemId}", id);
            return await _db.OrderItems
                .AsNoTracking()
                .Include(oi => oi.MenuItem)
                .FirstOrDefaultAsync(oi => oi.Id == id);
        }

        public async Task<List<OrderItem>> GetByOrderIdAsync(int orderId)
        {
            _logger.LogDebug("Fetching order items for order {OrderId}", orderId);
            return await _db.OrderItems
                .AsNoTracking()
                .Include(oi => oi.MenuItem)
                .Where(oi => oi.OrderId == orderId)
                .ToListAsync();
        }

        public async Task<OrderItem> AddAsync(OrderItem orderItem)
        {
            _logger.LogInformation("Adding order item to order {OrderId}, MenuItemId: {MenuItemId}", orderItem.OrderId, orderItem.MenuItemId);
            _db.OrderItems.Add(orderItem);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Added order item with id {OrderItemId}", orderItem.Id);
            return orderItem;
        }

        public async Task<bool> UpdateAsync(OrderItem orderItem)
        {
            _logger.LogInformation("Updating order item {OrderItemId}", orderItem.Id);
            var existing = await _db.OrderItems.FindAsync(orderItem.Id);
            if (existing == null)
            {
                _logger.LogWarning("Cannot update order item {OrderItemId}: not found", orderItem.Id);
                return false;
            }

            existing.Quantity = orderItem.Quantity;
            existing.UnitPrice = orderItem.UnitPrice;
            existing.SpecialInstructions = orderItem.SpecialInstructions;

            await _db.SaveChangesAsync();
            _logger.LogInformation("Successfully updated order item {OrderItemId}", orderItem.Id);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            _logger.LogInformation("Soft-deleting order item {OrderItemId}", id);
            var existing = await _db.OrderItems.FindAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("Cannot delete order item {OrderItemId}: not found", id);
                return false;
            }

            existing.IsArchived = true;
            await _db.SaveChangesAsync();
            _logger.LogInformation("Successfully soft-deleted order item {OrderItemId}", id);
            return true;
        }
    }
}
