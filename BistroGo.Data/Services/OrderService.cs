using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;

namespace BistroGo.Data.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orders;

        public OrderService(IOrderRepository orders)
        {
            _orders = orders;
        }

        public async Task<PlaceOrderResult> PlaceOrderAsync(int id)
        {
            var order = await _orders.GetByIdAsync(id);
            if (order == null) return PlaceOrderResult.NotFound;
            if (order.IsFinalized) return PlaceOrderResult.AlreadyPlaced;

            // The database requires a pickup slot on every placed order (CK_Orders_FinalizedConsistent).
            if (order.PickupSlotId == null) return PlaceOrderResult.PickupSlotRequired;

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