using BistroGo.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace BistroGo.Data.DataAccess
{
    /// <summary>
    /// Reference data that must exist for the app to work.
    /// HasData rows are written INTO the migration, so every environment that runs
    /// `dotnet ef database update` gets exactly the same rows with the same ids.
    ///
    /// Roles are NOT seeded here (they're created at startup
    /// by RoleManager in BistroGo.Web/Program.cs).
    /// </summary>
    public static class SeedData
    {
        public static void Apply(ModelBuilder builder)
        {
            SeedOrderStatuses(builder);
            SeedMenuCategories(builder);
            SeedMenuItems(builder);
        }

        // Ids come from OrderStatusIds so code and data can never drift apart.
        private static void SeedOrderStatuses(ModelBuilder builder)
        {
            builder.Entity<OrderStatus>().HasData(
                new OrderStatus { Id = OrderStatusIds.Received,  Name = "Received"  },
                new OrderStatus { Id = OrderStatusIds.Preparing, Name = "Preparing" },
                new OrderStatus { Id = OrderStatusIds.Ready,     Name = "Ready"     },
                new OrderStatus { Id = OrderStatusIds.PickedUp,  Name = "PickedUp"  },
                new OrderStatus { Id = OrderStatusIds.Cancelled, Name = "Cancelled" }
            );
        }

        // Starter categories. Same ids as InMemoryMenuCategoryRepository so the
        // in-memory menu items (CategoryId 1-4) line up when we switch to EF.
        // Managers can add more at runtime; new ones get ids after these.
        private static void SeedMenuCategories(ModelBuilder builder)
        {
            builder.Entity<MenuCategory>().HasData(
                new MenuCategory { Id = 1, Name = "Appetizer", DisplayOrder = 1 },
                new MenuCategory { Id = 2, Name = "Entree",    DisplayOrder = 2 },
                new MenuCategory { Id = 3, Name = "Dessert",   DisplayOrder = 3 },
                new MenuCategory { Id = 4, Name = "Drink",     DisplayOrder = 4 }
            );
        }

        // Starter menu. Same ids/names/prices as MenuStore.Items so carts line up.
        // Needed because OrderItems.MenuItemId is a FK to this table.
        private static void SeedMenuItems(ModelBuilder builder)
        {
            builder.Entity<MenuItem>().HasData(
                new MenuItem { Id = 1, CategoryId = 1, Name = "Bruschetta", Description = "Grilled bread, tomatoes, basil", Price = 8.50m, IsAvailable = true },
                new MenuItem { Id = 2, CategoryId = 1, Name = "Caesar Salad", Description = "Romaine, parmesan, croutons", Price = 10.00m, IsAvailable = true },
                new MenuItem { Id = 3, CategoryId = 2, Name = "Margherita Pizza", Description = "Tomato, mozzarella, basil", Price = 14.00m, IsAvailable = true },
                new MenuItem { Id = 4, CategoryId = 2, Name = "Grilled Salmon", Description = "Lemon butter, seasonal veg", Price = 22.00m, IsAvailable = true },
                new MenuItem { Id = 5, CategoryId = 2, Name = "Ribeye Steak", Description = "12oz, garlic butter", Price = 28.00m, IsAvailable = true },
                new MenuItem { Id = 6, CategoryId = 3, Name = "Tiramisu", Description = "Classic Italian dessert", Price = 9.00m, IsAvailable = true },
                new MenuItem { Id = 7, CategoryId = 4, Name = "Espresso", Description = "Double shot", Price = 4.00m, IsAvailable = true },
                new MenuItem { Id = 8, CategoryId = 4, Name = "House Red", Description = "Glass of Cabernet", Price = 11.00m, IsAvailable = true }
            );
        }
    }
}
