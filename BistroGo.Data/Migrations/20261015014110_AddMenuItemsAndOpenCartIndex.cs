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
            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "CategoryId", "CreatedByUserId", "Description", "ImageUrl", "IsAvailable", "Name", "Price" },
                values: new object[,]
                {
                    { 1, 1, null, "Grilled bread, tomatoes, basil", null, true, "Bruschetta", 8.50m },
                    { 2, 1, null, "Romaine, parmesan, croutons", null, true, "Caesar Salad", 10.00m },
                    { 3, 2, null, "Tomato, mozzarella, basil", null, true, "Margherita Pizza", 14.00m },
                    { 4, 2, null, "Lemon butter, seasonal veg", null, true, "Grilled Salmon", 22.00m },
                    { 5, 2, null, "12oz, garlic butter", null, true, "Ribeye Steak", 28.00m },
                    { 6, 3, null, "Classic Italian dessert", null, true, "Tiramisu", 9.00m },
                    { 7, 4, null, "Double shot", null, true, "Espresso", 4.00m },
                    { 8, 4, null, "Glass of Cabernet", null, true, "House Red", 11.00m }
                });

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

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 8);
        }
    }
}
