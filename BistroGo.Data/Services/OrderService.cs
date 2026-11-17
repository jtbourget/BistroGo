using BistroGo.Core.Enumerations;
using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Core.Scheduling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BistroGo.Data.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orders;
        private readonly IPickupTimeSlotRepository _slots;
        private readonly TimeProvider _time;
        private readonly TimeSpan _leadTime;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IOrderRepository orders,
            IPickupTimeSlotRepository slots,
            TimeProvider time,
            IOptions<PickupSlotOptions> options,
            ILogger<OrderService>? logger = null)
        {
            _orders = orders;
            _slots = slots;
            _time = time;
            _leadTime = TimeSpan.FromMinutes(options.Value.LeadTimeMinutes);
            _logger = logger ?? NullLogger<OrderService>.Instance;
        }

        public async Task<PlaceOrderResult> PlaceOrderAsync(int id)
        {
            _logger.LogInformation("Processing order placement for order {OrderId}", id);

            var order = await _orders.GetByIdAsync(id);
            if (order == null)
            {
                _logger.LogWarning("Order placement failed: order {OrderId} not found", id);
                return PlaceOrderResult.NotFound;
            }

            if (order.IsFinalized)
            {
                _logger.LogWarning("Order placement failed: order {OrderId} is already finalized", id);
                return PlaceOrderResult.AlreadyPlaced;
            }

            // The database requires a pickup slot on every placed order (CK_Orders_FinalizedConsistent).
            if (order.PickupSlotId == null)
            {
                _logger.LogWarning("Order placement failed: order {OrderId} has no pickup slot assigned", id);
                return PlaceOrderResult.PickupSlotRequired;
            }

            // The slot must still exist (archived slots are hidden by the repository)...
            var slot = await _slots.GetByIdAsync(order.PickupSlotId.Value);
            if (slot == null)
            {
                _logger.LogWarning("Order placement failed: pickup slot {SlotId} for order {OrderId} not found", order.PickupSlotId.Value, id);
                return PlaceOrderResult.SlotUnavailable;
            }

            // ...and it must still be far enough ahead. A customer may have picked it earlier
            // and taken a while to check out. Same rule the available-slots list uses.
            var localNow = _time.GetLocalNow().DateTime;
            if (!PickupSlotRules.IsBookable(slot, localNow, _leadTime))
            {
                _logger.LogWarning("Order placement failed: pickup slot {SlotId} for order {OrderId} is no longer bookable at {LocalNow} (lead time: {LeadTime})",
                    slot.Id, id, localNow, _leadTime);
                return PlaceOrderResult.SlotUnavailable;
            }

            // ...and have room. Booked orders count against MaxOrders.
            var booked = await _slots.CountBookedOrdersAsync(slot.Id);
            if (booked >= slot.MaxOrders)
            {
                _logger.LogWarning("Order placement failed: pickup slot {SlotId} is full ({Booked}/{MaxOrders})",
                    slot.Id, booked, slot.MaxOrders);
                return PlaceOrderResult.SlotFull;
            }

            order.IsFinalized = true;
            order.FinalizedAt = DateTime.UtcNow;
            order.StatusId = OrderStatusIds.Received;
            order.ConfirmationCode ??= GenerateConfirmationCode();

            _logger.LogInformation("Saving finalized order {OrderId} with confirmation code {ConfirmationCode}",
                id, order.ConfirmationCode);

            // Saves the order and re-checks capacity inside a transaction, in case another
            // customer took the last spot after our count above.
            var saved = await _orders.UpdateIfSlotHasRoomAsync(order, slot.MaxOrders);

            switch (saved)
            {
                case SlotSaveResult.Saved:
                    _logger.LogInformation("Order {OrderId} successfully placed with confirmation {ConfirmationCode}",
                        id, order.ConfirmationCode);
                    return PlaceOrderResult.Placed;
                case SlotSaveResult.SlotFull:
                    _logger.LogWarning("Order {OrderId} placement failed concurrently: slot {SlotId} became full during save",
                        id, slot.Id);
                    return PlaceOrderResult.SlotFull;
                default:
                    _logger.LogWarning("Order {OrderId} placement failed: order not found during transactional save", id);
                    return PlaceOrderResult.NotFound;
            }
        }

        private static string GenerateConfirmationCode() =>
            "BG-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
    }
}
