using BistroGo.Core.Models;

namespace BistroGo.Api.Contracts
{
    /// <summary>One bookable slot as the customer sees it.</summary>
    public record AvailableSlotResponse(
        int Id,
        DateOnly SlotDate,
        TimeOnly StartTime,
        TimeOnly EndTime,
        int MaxOrders, int
        SpotsRemaining
        );

    /// <summary>What a bulk "create the day's slots" request did.</summary>
    public record BulkSlotsResponse(int CreatedCount, int SkippedCount, IReadOnlyList<PickupTimeSlot> Created);
}