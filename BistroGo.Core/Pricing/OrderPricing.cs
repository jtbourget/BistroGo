using BistroGo.Core.Models;

namespace BistroGo.Core.Pricing
{
    /// <summary>
    /// The one place that decides how an order's subtotal, tax and total are calculated.
    /// Used by both the cart service and the order repository so they can never disagree.
    /// </summary>
    public static class OrderPricing
    {
        /// <summary>Sales tax rate applied to the subtotal (8%).</summary>
        public const decimal TaxRate = 0.08m;

        /// <summary>
        /// Recalculates Subtotal, Tax and Total from the order's active (non-archived) lines.
        /// An order with no active lines gets totals of zero.
        /// </summary>
        public static void Recalculate(Order order)
        {
            // Items can be null when an Order arrives deserialized from JSON.
            var subtotal = (order.Items ?? new List<OrderItem>())
                .Where(i => !i.IsArchived)
                .Sum(i => i.Quantity * i.UnitPrice);

            order.Subtotal = Math.Round(subtotal, 2);
            order.Tax = Math.Round(order.Subtotal * TaxRate, 2);
            order.Total = order.Subtotal + order.Tax;
        }
    }
}