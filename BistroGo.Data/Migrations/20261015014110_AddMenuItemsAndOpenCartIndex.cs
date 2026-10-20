using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BistroGo.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuItemsAndOpenCartIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId_OpenCart",
                table: "Orders",
                column: "UserId",
                unique: true,
                filter: "\"IsFinalized\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_UserId_OpenCart",
                table: "Orders");
        }
    }
}
