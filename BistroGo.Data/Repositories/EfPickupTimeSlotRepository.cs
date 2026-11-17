using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Data.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BistroGo.Data.Repositories
{
    public class EfPickupTimeSlotRepository : IPickupTimeSlotRepository
    {
        private readonly BistroGoDBContext _db;
        private readonly ILogger<EfPickupTimeSlotRepository> _logger;

        public EfPickupTimeSlotRepository(BistroGoDBContext db, ILogger<EfPickupTimeSlotRepository>? logger = null)
        {
            _db = db;
            _logger = logger ?? NullLogger<EfPickupTimeSlotRepository>.Instance;
        }

        public async Task<List<PickupTimeSlot>> GetAllAsync()
        {
            _logger.LogDebug("Fetching all pickup time slots");
            return await _db.PickupTimeSlots
                .AsNoTracking()
                .OrderBy(s => s.SlotDate)
                .ThenBy(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<PickupTimeSlot?> GetByIdAsync(int id)
        {
            _logger.LogDebug("Fetching pickup time slot with id {SlotId}", id);
            return await _db.PickupTimeSlots
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<PickupTimeSlot>> GetByDateAsync(DateOnly date)
        {
            _logger.LogDebug("Fetching pickup time slots for date {Date}", date);
            return await _db.PickupTimeSlots
                .AsNoTracking()
                .Where(s => s.SlotDate == date)
                .OrderBy(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<PickupTimeSlot> AddAsync(PickupTimeSlot slot)
        {
            _logger.LogInformation("Adding pickup time slot on {SlotDate} from {StartTime} to {EndTime}", slot.SlotDate, slot.StartTime, slot.EndTime);
            _db.PickupTimeSlots.Add(slot);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Added pickup time slot with id {SlotId}", slot.Id);
            return slot;
        }

        public async Task<bool> UpdateAsync(PickupTimeSlot slot)
        {
            _logger.LogInformation("Updating pickup time slot {SlotId}", slot.Id);
            var existing = await _db.PickupTimeSlots.FindAsync(slot.Id);
            if (existing == null)
            {
                _logger.LogWarning("Cannot update pickup slot {SlotId}: not found", slot.Id);
                return false;
            }

            existing.SlotDate = slot.SlotDate;
            existing.StartTime = slot.StartTime;
            existing.EndTime = slot.EndTime;
            existing.MaxOrders = slot.MaxOrders;

            await _db.SaveChangesAsync();
            _logger.LogInformation("Successfully updated pickup time slot {SlotId}", slot.Id);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            _logger.LogInformation("Soft-deleting pickup time slot {SlotId}", id);
            var existing = await _db.PickupTimeSlots.FindAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("Cannot delete pickup slot {SlotId}: not found", id);
                return false;
            }

            existing.IsArchived = true;
            await _db.SaveChangesAsync();
            _logger.LogInformation("Successfully soft-deleted pickup time slot {SlotId}", id);
            return true;
        }

        public async Task<int> CountBookedOrdersAsync(int slotId)
        {
            _logger.LogDebug("Counting booked orders for slot {SlotId}", slotId);
            return await _db.BookedOrders(slotId).CountAsync();
        }

        public async Task AddRangeAsync(IEnumerable<PickupTimeSlot> slots)
        {
            var slotList = slots.ToList();
            _logger.LogInformation("Adding range of {Count} pickup time slots", slotList.Count);
            _db.PickupTimeSlots.AddRange(slotList);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Successfully added range of {Count} pickup time slots", slotList.Count);
        }
    }
}
