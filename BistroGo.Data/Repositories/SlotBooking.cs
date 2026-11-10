using BistroGo.Core.Models;
using BistroGo.Data.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace BistroGo.Data.Repositories
{
    internal static class SlotBooking
    {
        /// <summary>
        /// Orders using up capacity in a slot: placed and not cancelled, archived ones
        /// included (a picked-up order still used its spot).
        /// </summary>
        public static IQueryable<Order> BookedOrders(this BistroGoDBContext db, int slotId) =>
            db.Orders
              .IgnoreQueryFilters()
              .Where(o => o.PickupSlotId == slotId
                       && o.IsFinalized
                       && o.StatusId != OrderStatusIds.Cancelled);
    }
}