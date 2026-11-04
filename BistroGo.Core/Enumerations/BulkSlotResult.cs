namespace BistroGo.Core.Enumerations
{
    /// <summary>Why a request to create a day's pickup slots succeeded or was refused.</summary>
    public enum BulkSlotResult
    {
        Created,
        DateInPast,
        InvalidHours,
        InvalidSlotLength,
        InvalidCapacity
    }
}
