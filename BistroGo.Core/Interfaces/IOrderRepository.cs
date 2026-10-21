using BistroGo.Core.Models;

namespace BistroGo.Core.Interfaces
{
    /// <summary>
    /// Contract for order data access and lifecycle operations.
    /// Supports order placement and line item management.
    /// </summary>
    public interface IOrderRepository
    {
        /// <summary>Retrieves all non-archived orders with statuses, pickup slots, and line items.</summary>
        Task<List<Order>> GetAllAsync();

        /// <summary>Retrieves a single order by identifier with related entities.</summary>
        Task<Order?> GetByIdAsync(int id);

        /// <summary>Retrieves all orders placed by a specific user.</summary>
        Task<List<Order>> GetByUserIdAsync(string userId);

        /// <summary>Creates a new order, recalculating subtotal, tax, and total amounts.</summary>
        Task<Order> AddAsync(Order order);

        /// <summary>Updates an existing order's metadata and recalculates totals.</summary>
        Task<bool> UpdateAsync(Order order);

        /// <summary>Soft-deletes an order by marking IsArchived = true and setting ArchivedAt timestamp.</summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>Finalizes a pending cart into a placed order with an assigned status and confirmation code.</summary>
        Task<bool> PlaceOrderAsync(int id);

        /// <summary>Retrieves all active line items for an order.</summary>
        Task<List<OrderItem>> GetLinesByOrderIdAsync(int orderId);

        /// <summary>Retrieves a single line item by identifier.</summary>
        Task<OrderItem?> GetLineByIdAsync(int id);

        /// <summary>Adds a new line item to an order and updates order totals.</summary>
        Task<OrderItem> AddLineAsync(OrderItem line);

        /// <summary>Updates quantity, unit price, or instructions for a line item and updates order totals.</summary>
        Task<bool> UpdateLineAsync(OrderItem line);

        /// <summary>Soft-deletes a line item and updates the parent order totals.</summary>
        Task<bool> DeleteLineAsync(int id);
    }
}
