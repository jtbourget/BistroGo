using BistroGo.Core.Identity;
using BistroGo.Core.Models;

namespace BistroGo.Core.Pricing
{
    /// <summary>
    /// The one place that decides how an order's subtotal, tax and total are calculated.
    /// Used by both the cart service and the order repository so they can never disagree.
    /// </summary>
    public static class OrderPricing
    {
        /// <summary>
        /// Recalculates Subtotal, Tax and Total from the order's active (non-archived) lines.
        /// An order with no active lines gets totals of zero.
        /// </summary>
        public static void Recalculate(Order order)
        {
            Recalculate(order, userRoles: null);
        }

        /// <summary>
        /// Recalculates Subtotal, Tax and Total from the order's active (non-archived) lines.
        /// Applies employee discount if the user has a qualifying role (KitchenStaff or Manager).
        /// An order with no active lines gets totals of zero.
        /// </summary>
        /// <param name="order">The order to recalculate.</param>
        /// <param name="userRoles">The user's roles (from UserManager.GetRolesAsync). If null, no discount applied.</param>
        public static void Recalculate(Order order, IEnumerable<string>? userRoles)
        {
            // Calculate base subtotal from active items
            var baseSubtotal = (order.Items ?? new List<OrderItem>())
                .Where(i => !i.IsArchived)
                .Sum(i => i.Quantity * i.UnitPrice);

            // Determine if user qualifies for employee discount
            var hasEmployeeDiscount = userRoles != null && UserHasEmployeeDiscount(userRoles);

            // Calculate discount amount and apply to subtotal if applicable
            var discountAmount = 0m;
            var discountedSubtotal = baseSubtotal;

            if (hasEmployeeDiscount)
            {
                discountAmount = Math.Round(baseSubtotal * PricingConstants.EmployeeDiscount, 2);
                discountedSubtotal = Math.Round(baseSubtotal - discountAmount, 2);
            }

            // Set order values
            order.Subtotal = baseSubtotal;
            order.DiscountAmount = discountAmount;
            order.Tax = Math.Round(discountedSubtotal * PricingConstants.TaxRate, 2);
            order.Total = discountedSubtotal + order.Tax;
        }

        /// <summary>
        /// Determines if a user with the given roles qualifies for the employee discount.
        /// Qualifies if user is in KitchenStaff or Manager role.
        /// </summary>
        private static bool UserHasEmployeeDiscount(IEnumerable<string> userRoles)
        {
            return userRoles.Contains(RoleNames.KitchenStaff) || userRoles.Contains(RoleNames.Manager);
        }
    }
}