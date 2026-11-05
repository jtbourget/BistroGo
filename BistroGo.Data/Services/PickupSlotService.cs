using BistroGo.Core.Enumerations;
using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;

namespace BistroGo.Data.Services
{
    public class PickupSlotService : IPickupSlotService
    {
        private readonly IPickupTimeSlotRepository _slots;
        private readonly TimeProvider _time;

        public PickupSlotService(IPickupTimeSlotRepository slots, TimeProvider time)
        {
            _slots = slots;
            _time = time;
        }

        // "Today" comes from the injected clock (not DateTime.Now) so tests can control it.
        private DateOnly Today => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

        public async Task<CreateSlotOutcome> CreateAsync(PickupTimeSlot slot)
        {
            if (slot.SlotDate < Today) return new(CreateSlotResult.DateInPast, null);
            if (slot.EndTime <= slot.StartTime) return new(CreateSlotResult.EndNotAfterStart, null);
            if (slot.MaxOrders < 1) return new(CreateSlotResult.InvalidCapacity, null);

            var existing = await _slots.GetByDateAsync(slot.SlotDate);
            if (existing.Any(s => s.StartTime == slot.StartTime)) return new(CreateSlotResult.Duplicate, null);

            // Build a fresh entity from the fields we accept, so a caller can't sneak in
            // an Id, IsArchived, or a list of Orders.
            var created = await _slots.AddAsync(new PickupTimeSlot
            {
                SlotDate = slot.SlotDate,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                MaxOrders = slot.MaxOrders
            });
            return new(CreateSlotResult.Created, created);
        }

        // Create day is not easily readable
        // Creates time slots for one day, back to back, refuses time slots that don't fit definition.
        public async Task<BulkSlotOutcome> CreateDayAsync(BulkSlotRequest request)
        {
            if (request.Date < Today) return Refused(BulkSlotResult.DateInPast);
            if (request.Closes <= request.Opens) return Refused(BulkSlotResult.InvalidHours);
            if (request.SlotMinutes is < 5 or > 240) return Refused(BulkSlotResult.InvalidSlotLength);
            if (request.MaxOrders < 1) return Refused(BulkSlotResult.InvalidCapacity);

            var existingStarts = (await _slots.GetByDateAsync(request.Date))
                .Select(s => s.StartTime)
                .ToHashSet();

            // Plain TimeSpan arithmetic: TimeOnly.AddMinutes wraps past midnight (23:45 + 30 min = 00:15),
            // which would quietly invent slots on the wrong side of the clock.
            var length = TimeSpan.FromMinutes(request.SlotMinutes);
            var closes = request.Closes.ToTimeSpan();

            var toCreate = new List<PickupTimeSlot>();
            var skipped = 0;

            // A slot only counts if it fits fully before closing time.
            for (var start = request.Opens.ToTimeSpan(); start + length <= closes; start += length)
            {
                var startTime = TimeOnly.FromTimeSpan(start);
                if (existingStarts.Contains(startTime))
                {
                    skipped++;
                    continue;
                }

                toCreate.Add(new PickupTimeSlot
                {
                    SlotDate = request.Date,
                    StartTime = startTime,
                    EndTime = TimeOnly.FromTimeSpan(start + length),
                    MaxOrders = request.MaxOrders
                });
            }

            if (toCreate.Count > 0) await _slots.AddRangeAsync(toCreate);

            return new BulkSlotOutcome(BulkSlotResult.Created, toCreate, skipped);
        }

        private static BulkSlotOutcome Refused(BulkSlotResult why) =>
            new(why, Array.Empty<PickupTimeSlot>(), 0);

        public async Task<IReadOnlyList<SlotAvailability>> GetAvailableAsync(DateOnly? date)
        {
            var now = _time.GetLocalNow().DateTime;
            var today = DateOnly.FromDateTime(now);
            var day = date ?? today;

            if (day < today) return Array.Empty<SlotAvailability>();

            // Move _time.GetLocalNow().DateTime as parameter in FromDateTime method?
            var timeNow = TimeOnly.FromDateTime(now);
            var available = new List<SlotAvailability>();

            // GetByDateAsync already leaves out archived slots (the DbContext's query filter).
            foreach (var slot in await _slots.GetByDateAsync(day))
            {
                if (day == today && slot.StartTime <= timeNow) continue;   // already started

                var remaining = slot.MaxOrders - await _slots.CountBookedOrdersAsync(slot.Id);
                if (remaining > 0) available.Add(new SlotAvailability(slot, remaining));
            }

            return available;
        }
    }
}