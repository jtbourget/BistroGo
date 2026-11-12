using BistroGo.Core.Enumerations;
using BistroGo.Core.Models;

namespace BistroGo.Core.Interfaces
{
    public record CreateSlotOutcome(CreateSlotResult Result, PickupTimeSlot? Slot);

    /// <summary>"Open from 11:00 to 20:00, a slot every 30 minutes, 10 orders each."</summary>
    public record BulkSlotRequest(DateOnly Date, TimeOnly Opens, TimeOnly Closes, int SlotMinutes, int MaxOrders);

    public record BulkSlotOutcome(BulkSlotResult Result, IReadOnlyList<PickupTimeSlot> Created, int Skipped);

    /// <summary>A slot a customer can still book, and how many spots are left in it.</summary>
    public record SlotAvailability(PickupTimeSlot Slot, int Remaining);

    /// <summary>Business rules for managing pickup slots.</summary>
    public interface IPickupSlotService
    {
        Task<CreateSlotOutcome> CreateAsync(PickupTimeSlot slot);

        /// <summary>Creates a day's slots. Slots that already exist are skipped, the rest are created.</summary>
        Task<BulkSlotOutcome> CreateDayAsync(BulkSlotRequest request);

        /// <summary>
        /// Slots on a day that can still be booked: not archived, starting at least the
        /// configured lead time from now, and not full. A null date means today.
        /// </summary>
        Task<IReadOnlyList<SlotAvailability>> GetAvailableAsync(DateOnly? date);
    }
}