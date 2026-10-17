using BistroGo.Core.Models;

namespace BistroGo.Core.Interfaces
{
    public interface ICartService
    {
        // The owner's open cart with Items (and each MenuItem) loaded, or null if none yet.
        Task<Order?> GetCartAsync(CartOwner owner);

        // Adds a menu item (creating the cart if needed). False if the item is missing/unavailable.
        Task<bool> AddItemAsync(CartOwner owner, int menuItemId, int quantity = 1);

        // Sets an exact quantity; below 1 removes the line. False if it isn't in the cart.
        Task<bool> SetQuantityAsync(CartOwner owner, int menuItemId, int quantity);

        Task<bool> RemoveItemAsync(CartOwner owner, int menuItemId);

        // On login: fold the guest cart into the user's cart, then delete the guest cart.
        Task MergeGuestCartAsync(Guid guestToken, string userId);
    }
}