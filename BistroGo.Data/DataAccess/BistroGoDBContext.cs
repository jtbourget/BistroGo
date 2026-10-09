
﻿using BistroGo.Core.Models;
using BistroGo.Data.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BistroGo.Data.DataAccess
{
    /// <summary>
    /// EF Core database context for BistroGo.
    ///
    /// Most of the schema (keys, FKs, max lengths, precision, unique indexes, delete rules)
    /// comes from the data annotation attributes on the models in BistroGo.Core.
    /// This class only configures what attributes CAN'T express:
    ///   1. Foreign keys to AspNetUsers (Core can't see ApplicationUser)
    ///   2. Database default values
    ///   3. CHECK constraints
    /// </summary>
    public class BistroGoDBContext : IdentityDbContext<ApplicationUser>
    {
        public BistroGoDBContext(DbContextOptions<BistroGoDBContext> options) : base(options)
        {
        }

        // One DbSet per table. Identity's tables (AspNetUsers, AspNetRoles,
        // AspNetUserRoles, ...) come from IdentityDbContext automatically.
        public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
        public DbSet<MenuItem> MenuItems => Set<MenuItem>();
        public DbSet<PickupTimeSlot> PickupTimeSlots => Set<PickupTimeSlot>();
        public DbSet<OrderStatus> OrderStatuses => Set<OrderStatus>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<Rating> Ratings => Set<Rating>();
        public DbSet<Expense> Expenses => Set<Expense>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // MUST run first: this is where Identity configures its own tables.
            base.OnModelCreating(builder);

            ConfigureMenu(builder);
            ConfigurePickupTimeSlots(builder);
            ConfigureOrders(builder);
            ConfigureOrderItems(builder);
            ConfigureRatings(builder);
            ConfigureExpenses(builder);

            // Reference data (OrderStatuses, MenuCategories) - see SeedData.cs
            SeedData.Apply(builder);
        }

        // ------------------------------------------------------------------
        // MenuCategories + MenuItems
        // ------------------------------------------------------------------
        private static void ConfigureMenu(ModelBuilder builder)
        {
            builder.Entity<MenuCategory>(category =>
            {
                category.Property(c => c.DisplayOrder)
                        .HasDefaultValue(0);
            });

            builder.Entity<MenuItem>(item =>
            {
                // FK -> AspNetUsers. Deleting a user keeps the menu item, just clears who made it.
                item.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(i => i.CreatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Default true. HasSentinel(true) tells EF "true means not explicitly set",
                // so an explicit false is still sent to the database (see notes in chat).
                item.Property(i => i.IsAvailable)
                    .HasDefaultValue(true)
                    .HasSentinel(true);

                // SQLite stores decimals as TEXT, so cast to compare as a number.
                item.ToTable(t => t.HasCheckConstraint(
                    "CK_MenuItems_Price_NonNegative",
                    "CAST(\"Price\" AS REAL) >= 0"));
            });
        }

        // ------------------------------------------------------------------
        // PickupTimeSlots
        // ------------------------------------------------------------------
        private static void ConfigurePickupTimeSlots(ModelBuilder builder)
        {
            builder.Entity<PickupTimeSlot>(slot =>
            {
                slot.Property(s => s.MaxOrders)
                    .HasDefaultValue(10);

                slot.ToTable(t =>
                {
                    // TimeOnly is stored as zero-padded 'HH:mm:ss' text, so text comparison works.
                    t.HasCheckConstraint(
                        "CK_PickupTimeSlots_EndAfterStart",
                        "\"EndTime\" > \"StartTime\"");

                    t.HasCheckConstraint(
                        "CK_PickupTimeSlots_MaxOrders_Positive",
                        "\"MaxOrders\" > 0");
                });
            });
        }

        // ------------------------------------------------------------------
        // Orders (cart + placed order + archive)
        // ------------------------------------------------------------------
        private static void ConfigureOrders(ModelBuilder builder)
        {
            builder.Entity<Order>(order =>
            {
                // FK -> AspNetUsers. Deleting a user keeps their order history.
                order.HasOne<ApplicationUser>()
                     .WithMany()
                     .HasForeignKey(o => o.UserId)
                     .OnDelete(DeleteBehavior.SetNull);

                // Defaults
                order.Property(o => o.IsFinalized).HasDefaultValue(false);
                order.Property(o => o.IsArchived).HasDefaultValue(false);
                order.Property(o => o.Subtotal).HasDefaultValue(0m);
                order.Property(o => o.Tax).HasDefaultValue(0m);
                order.Property(o => o.Total).HasDefaultValue(0m);
                order.Property(o => o.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                order.ToTable(t =>
                {
                    t.HasCheckConstraint(
                        "CK_Orders_Amounts_NonNegative",
                        "CAST(\"Subtotal\" AS REAL) >= 0 AND CAST(\"Tax\" AS REAL) >= 0 AND CAST(\"Total\" AS REAL) >= 0");

                    // A cart has none of the checkout fields; a placed order has all of them.
                    t.HasCheckConstraint(
                        "CK_Orders_FinalizedConsistent",
                        "(\"IsFinalized\" = 0 AND \"FinalizedAt\" IS NULL AND \"StatusId\" IS NULL AND \"ConfirmationCode\" IS NULL)" +
                        " OR " +
                        "(\"IsFinalized\" = 1 AND \"FinalizedAt\" IS NOT NULL AND \"StatusId\" IS NOT NULL AND \"ConfirmationCode\" IS NOT NULL AND \"PickupSlotId\" IS NOT NULL)");

                    // Only placed orders can be archived (abandoned carts get deleted instead),
                    // and the flag must agree with the timestamp.
                    t.HasCheckConstraint(
                        "CK_Orders_ArchivedConsistent",
                        "(\"IsArchived\" = 0 AND \"ArchivedAt\" IS NULL)" +
                        " OR " +
                        "(\"IsArchived\" = 1 AND \"ArchivedAt\" IS NOT NULL AND \"IsFinalized\" = 1)");
                });
            });
        }

        // ------------------------------------------------------------------
        // OrderItems
        // ------------------------------------------------------------------
        private static void ConfigureOrderItems(ModelBuilder builder)
        {
            builder.Entity<OrderItem>(line =>
            {
                line.Property(l => l.Quantity)
                    .HasDefaultValue(1);

                line.ToTable(t =>
                {
                    t.HasCheckConstraint(
                        "CK_OrderItems_Quantity_Positive",
                        "\"Quantity\" > 0");

                    t.HasCheckConstraint(
                        "CK_OrderItems_UnitPrice_NonNegative",
                        "CAST(\"UnitPrice\" AS REAL) >= 0");
                });
            });
        }

        // ------------------------------------------------------------------
        // Ratings
        // ------------------------------------------------------------------
        private static void ConfigureRatings(ModelBuilder builder)
        {
            builder.Entity<Rating>(rating =>
            {
                rating.Property(r => r.SubmittedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");

                rating.ToTable(t => t.HasCheckConstraint(
                    "CK_Ratings_Stars_Range",
                    "\"Stars\" BETWEEN 1 AND 5"));
            });
        }

        // ------------------------------------------------------------------
        // Expenses
        // ------------------------------------------------------------------
        private static void ConfigureExpenses(ModelBuilder builder)
        {
            builder.Entity<Expense>(expense =>
            {
                // FK -> AspNetUsers. Restrict: you can't delete a user who has recorded
                // expenses - the books must always say who recorded each one.
                expense.HasOne<ApplicationUser>()
                       .WithMany()
                       .HasForeignKey(e => e.RecordedByUserId)
                       .OnDelete(DeleteBehavior.Restrict);

                expense.ToTable(t => t.HasCheckConstraint(
                    "CK_Expenses_Amount_NonNegative",
                    "CAST(\"Amount\" AS REAL) >= 0"));
            });
        }
    }
}
