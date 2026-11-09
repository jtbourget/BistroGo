using BistroGo.Core.Enumerations;

namespace BistroGo.Core.Interfaces
{
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