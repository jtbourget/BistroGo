using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BistroGo.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsArchivedSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Reviews",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "PickupTimeSlots",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "OrderItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "MenuItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "MenuCategories",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Expenses",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "PickupTimeSlots");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "MenuCategories");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Expenses");
        }
    }
}
