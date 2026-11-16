namespace BistroGo.Core.Interfaces
{
    /// <summary>
    /// Service for managing and retrieving the current sales tax rate.
    /// The rate is maintained in memory by the server to prevent client tampering.
    /// </summary>
    public interface ITaxRateService
    {
        /// <summary>
        /// Retrieves the current active tax rate.
        /// </summary>
        /// <returns>The tax rate as a decimal (e.g., 0.08 for 8%).</returns>
        decimal GetCurrentRate();

        /// <summary>
        /// Updates the current active tax rate.
        /// </summary>
        /// <param name="rate">The new tax rate as a decimal (e.g., 0.09 for 9%). Must be between 0 and 0.20.</param>
        /// <returns>True if update succeeded; false if validation failed.</returns>
        bool UpdateRate(decimal rate);
    }
}
