namespace BistroGo.Core.Pricing
{
    /// <summary>
    /// Configuration constants for order pricing.
    /// Centralized location for easy adjustment of pricing parameters.
    /// </summary>
    public static class PricingConstants
    {
        /// <summary>Sales tax rate applied to the subtotal (8%).</summary>
        public const decimal TaxRate = 0.08m;

        /// <summary>
        /// Employee discount percentage for qualifying roles (KitchenStaff, Manager).
        /// 0.2 = 20% discount (customer pays 80% of original price).
        /// Adjust this value to change the employee discount percentage.
        /// Example: 0.2 means 20% off, 0.15 means 15% off, etc.
        /// </summary>
        public const decimal EmployeeDiscount = 0.2m;
    }
}
