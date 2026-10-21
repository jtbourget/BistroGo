using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Data.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace BistroGo.Data.Repositories
{
    public class EfPickupTimeSlotRepository : IPickupTimeSlotRepository
    {
        private readonly BistroGoDBContext _db;

        public EfPickupTimeSlotRepository(BistroGoDBContext db)
        {
            _db = db;
        }

        public async Task<List<PickupTimeSlot>> GetAllAsync()
        {
            return await _db.PickupTimeSlots
                .AsNoTracking()
                .OrderBy(s => s.SlotDate)
                .ThenBy(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<PickupTimeSlot?> GetByIdAsync(int id)
        {
            return await _db.PickupTimeSlots
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<PickupTimeSlot>> GetByDateAsync(DateOnly date)
        {
            return await _db.PickupTimeSlots
                .AsNoTracking()
                .Where(s => s.SlotDate == date)
                .OrderBy(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<PickupTimeSlot> AddAsync(PickupTimeSlot slot)
        {
            _db.PickupTimeSlots.Add(slot);
            await _db.SaveChangesAsync();
            return slot;
        }

        public async Task<bool> UpdateAsync(PickupTimeSlot slot)
        {
            var existing = await _db.PickupTimeSlots.FindAsync(slot.Id);
            if (existing == null) return false;

            existing.SlotDate = slot.SlotDate;
            existing.StartTime = slot.StartTime;
            existing.EndTime = slot.EndTime;
            existing.MaxOrders = slot.MaxOrders;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _db.PickupTimeSlots.FindAsync(id);
            if (existing == null) return false;

            existing.IsArchived = true;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
