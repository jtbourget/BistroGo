using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BistroGo.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedMenuItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: the menu items are already seeded by
            // 20261015014110_AddMenuItemsAndOpenCartIndex. Seeding them again here
            // caused "UNIQUE constraint failed: MenuItems.Id".
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: rolling this migration back must not delete
            // the menu items that the earlier migration owns.
        }
    }
}
