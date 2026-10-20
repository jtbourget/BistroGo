using BistroGo.Core.Models;

namespace BistroGo.Core.Interfaces
{
    /// <summary>
    /// Contract for direct operations on individual order line items.
    /// </summary>
    public interface IOrderItemRepository
    {
        /// <summary>Retrieves all non-archived order line items.</summary>
        Task<List<OrderItem>> GetAllAsync();

        /// <summary>Retrieves a single line item by identifier with associated menu item.</summary>
        Task<OrderItem?> GetByIdAsync(int id);

        /// <summary>Retrieves all line items belonging to a specific order.</summary>
        Task<List<OrderItem>> GetByOrderIdAsync(int orderId);

        /// <summary>Adds a new line item to the database.</summary>
        Task<OrderItem> AddAsync(OrderItem orderItem);

        /// <summary>Updates an existing line item.</summary>
        Task<bool> UpdateAsync(OrderItem orderItem);

        /// <summary>Soft-deletes an order line item by setting IsArchived = true.</summary>
        Task<bool> DeleteAsync(int id);
    }
}
