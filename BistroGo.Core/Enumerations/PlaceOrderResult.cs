namespace BistroGo.Core.Enumerations
{
    /// <summary>Why a place-order request succeeded or was refused.</summary>
    public enum PlaceOrderResult
    {
        Placed,
        NotFound,
        AlreadyPlaced,
        PickupSlotRequired,
        SlotUnavailable,
        SlotFull
    }
}
