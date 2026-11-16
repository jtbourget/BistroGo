namespace BistroGo.Core.Scheduling;

/// <summary>
/// Pickup-slot settings read from the "PickupSlots" section of appsettings.json.
/// </summary>
public class PickupSlotOptions
{
    public const string SectionName = "PickupSlots";

    /// <summary>
    /// How many minutes of notice the kitchen needs. A slot is only bookable
    /// if it starts at least this long from now.
    /// </summary>
    public int LeadTimeMinutes { get; set; } = 15;
}