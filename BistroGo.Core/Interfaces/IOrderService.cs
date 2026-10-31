namespace BistroGo.Core.Interfaces
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

    /// <summary>
    /// Business workflows for orders. Repositories only load and save data;
    /// the rules about what an order needs before it can be placed live here.
    /// </summary>
    public interface IOrderService
    {
        /// <summary>
        /// Finalizes a pending order: status Received, confirmation code, timestamp.
        /// Returns why it was refused if it can't be placed.
        /// </summary>
        Task<PlaceOrderResult> PlaceOrderAsync(int id);
    }
}