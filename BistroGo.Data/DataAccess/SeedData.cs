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
    }
}
