namespace BistroGo.Core.Enumerations
{
    /// <summary>Why a request to create a single pickup slot succeeded or was refused.</summary>
    public enum CreateSlotResult
    {
        Created,
        DateInPast,
        EndNotAfterStart,
        InvalidCapacity,
        Duplicate
    }
}
