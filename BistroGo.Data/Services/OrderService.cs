using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;

namespace BistroGo.Data.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orders;
        private readonly IPickupTimeSlotRepository _slots;

        public OrderService(IOrderRepository orders, IPickupTimeSlotRepository slots)
        {
            _orders = orders;
            _slots = slots;
        }

        public async Task<PlaceOrderResult> PlaceOrderAsync(int id)
        {
            var order = await _orders.GetByIdAsync(id);
            if (order == null) return PlaceOrderResult.NotFound;
            if (order.IsFinalized) return PlaceOrderResult.AlreadyPlaced;

            // The database requires a pickup slot on every placed order (CK_Orders_FinalizedConsistent).
            if (order.PickupSlotId == null) return PlaceOrderResult.PickupSlotRequired;

            // The slot must still exist (archived slots are hidden by the repository)...
            var slot = await _slots.GetByIdAsync(order.PickupSlotId.Value);
            if (slot == null) return PlaceOrderResult.SlotUnavailable;

            // ...and have room. Booked orders count against MaxOrders.
            var booked = await _slots.CountBookedOrdersAsync(slot.Id);
            if (booked >= slot.MaxOrders) return PlaceOrderResult.SlotFull;

            order.IsFinalized = true;
            order.FinalizedAt = DateTime.UtcNow;
            order.StatusId = OrderStatusIds.Received;
            order.ConfirmationCode ??= GenerateConfirmationCode();

            // UpdateAsync saves these fields and recalculates the totals.
            return await _orders.UpdateAsync(order) ? PlaceOrderResult.Placed : PlaceOrderResult.NotFound;
        }

        private static string GenerateConfirmationCode() =>
            "BG-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
    }
}