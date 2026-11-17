using BistroGo.Core.Enumerations;
using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Core.Scheduling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BistroGo.Data.Services
{
    public class PickupSlotService : IPickupSlotService
    {
        private readonly IPickupTimeSlotRepository _slots;
        private readonly TimeProvider _time;
        private readonly TimeSpan _leadTime;
        private readonly ILogger<PickupSlotService> _logger;

        public PickupSlotService(
            IPickupTimeSlotRepository slots,
            TimeProvider time,
            IOptions<PickupSlotOptions> options,
            ILogger<PickupSlotService>? logger = null)
        {
            _slots = slots;
            _time = time;
            _leadTime = TimeSpan.FromMinutes(options.Value.LeadTimeMinutes);
            _logger = logger ?? NullLogger<PickupSlotService>.Instance;
        }

        // "Today" comes from the injected clock (not DateTime.Now) so tests can control it.
        private DateOnly Today => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

        public async Task<CreateSlotOutcome> CreateAsync(PickupTimeSlot slot)
        {
            _logger.LogInformation("Creating slot on {SlotDate} ({StartTime}-{EndTime}), MaxOrders: {MaxOrders}",
                slot.SlotDate, slot.StartTime, slot.EndTime, slot.MaxOrders);

            if (slot.SlotDate < Today)
            {
                _logger.LogWarning("Refused slot creation: date {SlotDate} is in the past (Today is {Today})", slot.SlotDate, Today);
                return new(CreateSlotResult.DateInPast, null);
            }
            if (slot.EndTime <= slot.StartTime)
            {
                _logger.LogWarning("Refused slot creation: end time {EndTime} must be after start time {StartTime}", slot.EndTime, slot.StartTime);
                return new(CreateSlotResult.EndNotAfterStart, null);
            }
            if (slot.MaxOrders < 1)
            {
                _logger.LogWarning("Refused slot creation: invalid capacity {MaxOrders}", slot.MaxOrders);
                return new(CreateSlotResult.InvalidCapacity, null);
            }

            var existing = await _slots.GetByDateAsync(slot.SlotDate);
            if (existing.Any(s => s.StartTime == slot.StartTime))
            {
                _logger.LogWarning("Refused slot creation: duplicate slot for date {SlotDate} and time {StartTime}", slot.SlotDate, slot.StartTime);
                return new(CreateSlotResult.Duplicate, null);
            }

            // Build a fresh entity from the fields we accept, so a caller can't sneak in
            // an Id, IsArchived, or a list of Orders.
            var created = await _slots.AddAsync(new PickupTimeSlot
            {
                SlotDate = slot.SlotDate,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                MaxOrders = slot.MaxOrders
            });

            _logger.LogInformation("Successfully created slot {SlotId} for {SlotDate} at {StartTime}", created.Id, created.SlotDate, created.StartTime);
            return new(CreateSlotResult.Created, created);
        }

        public async Task<BulkSlotOutcome> CreateDayAsync(BulkSlotRequest request)
        {
            _logger.LogInformation("Bulk creating slots for date {Date}: {Opens} to {Closes}, length: {SlotMinutes}min, capacity: {MaxOrders}",
                request.Date, request.Opens, request.Closes, request.SlotMinutes, request.MaxOrders);

            if (request.Date < Today)
            {
                _logger.LogWarning("Refused bulk slot creation: date {Date} is in the past", request.Date);
                return Refused(BulkSlotResult.DateInPast);
            }
            if (request.Closes <= request.Opens)
            {
                _logger.LogWarning("Refused bulk slot creation: closing time {Closes} must be after opening {Opens}", request.Closes, request.Opens);
                return Refused(BulkSlotResult.InvalidHours);
            }
            if (request.SlotMinutes is < 5 or > 240)
            {
                _logger.LogWarning("Refused bulk slot creation: invalid slot length {SlotMinutes}", request.SlotMinutes);
                return Refused(BulkSlotResult.InvalidSlotLength);
            }
            if (request.MaxOrders < 1)
            {
                _logger.LogWarning("Refused bulk slot creation: invalid capacity {MaxOrders}", request.MaxOrders);
                return Refused(BulkSlotResult.InvalidCapacity);
            }

            var existingStarts = (await _slots.GetByDateAsync(request.Date))
                .Select(s => s.StartTime)
                .ToHashSet();

            var length = TimeSpan.FromMinutes(request.SlotMinutes);
            var closes = request.Closes.ToTimeSpan();

            var toCreate = new List<PickupTimeSlot>();
            var skipped = 0;

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

            if (toCreate.Count > 0)
            {
                await _slots.AddRangeAsync(toCreate);
            }

            _logger.LogInformation("Bulk slot creation finished for {Date}: {CreatedCount} created, {SkippedCount} skipped",
                request.Date, toCreate.Count, skipped);

            return new BulkSlotOutcome(BulkSlotResult.Created, toCreate, skipped);
        }

        private static BulkSlotOutcome Refused(BulkSlotResult why) =>
            new(why, Array.Empty<PickupTimeSlot>(), 0);

        public async Task<IReadOnlyList<SlotAvailability>> GetAvailableAsync(DateOnly? date)
        {
            var now = _time.GetLocalNow().DateTime;
            var today = DateOnly.FromDateTime(now);
            var day = date ?? today;

            _logger.LogDebug("Fetching available slots for date {Date} (current time: {Now})", day, now);

            if (day < today)
            {
                _logger.LogDebug("Requested date {Date} is in past; returning 0 available slots", day);
                return Array.Empty<SlotAvailability>();
            }

            var available = new List<SlotAvailability>();

            // GetByDateAsync already leaves out archived slots (the DbContext's query filter).
            foreach (var slot in await _slots.GetByDateAsync(day))
            {
                // Same rule order placement will use (step 4), so the list and the check can't disagree.
                if (!PickupSlotRules.IsBookable(slot, now, _leadTime)) continue;

                var remaining = slot.MaxOrders - await _slots.CountBookedOrdersAsync(slot.Id);
                if (remaining > 0) available.Add(new SlotAvailability(slot, remaining));
            }

            _logger.LogDebug("Found {Count} available slots for date {Date}", available.Count, day);
            return available;
        }

        public async Task<IReadOnlyList<SlotAvailability>> GetUpcomingAsync(int days)
        {
            _logger.LogDebug("Fetching upcoming available slots for next {Days} days", days);
            var result = new List<SlotAvailability>();
            for (var i = 0; i < days; i++)
                result.AddRange(await GetAvailableAsync(Today.AddDays(i)));
            return result;
        }
    }
}
