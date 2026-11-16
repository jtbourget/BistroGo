using BistroGo.Core.Models;

namespace BistroGo.Core.Scheduling;

/// <summary>
/// Rules about when a pickup slot can be booked. Kept in one place so the
/// available-slots list and order placement can't disagree.
/// </summary>
public static class PickupSlotRules
{
    /// <summary>
    /// True if the slot starts at least <paramref name="leadTime"/> after
    /// <paramref name="localNow"/>. Both are local wall-clock times.
    /// </summary>
    public static bool IsBookable(PickupTimeSlot slot, DateTime localNow, TimeSpan leadTime)
    {
        if (leadTime < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(leadTime), "Lead time can't be negative.");

        // SlotDate and StartTime are stored separately; combine them into one point in time.
        var slotStart = slot.SlotDate.ToDateTime(slot.StartTime);

        // A slot starting exactly at now + lead time is still bookable.
        return slotStart >= localNow + leadTime;
    }
}