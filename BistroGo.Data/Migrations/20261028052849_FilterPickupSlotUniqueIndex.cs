using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BistroGo.Data.Migrations
{
    /// <inheritdoc />
    public partial class FilterPickupSlotUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PickupTimeSlots_SlotDate_StartTime",
                table: "PickupTimeSlots");

            migrationBuilder.CreateIndex(
                name: "IX_PickupTimeSlots_SlotDate_StartTime_Active",
                table: "PickupTimeSlots",
                columns: new[] { "SlotDate", "StartTime" },
                unique: true,
                filter: "\"IsArchived\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PickupTimeSlots_SlotDate_StartTime_Active",
                table: "PickupTimeSlots");

            migrationBuilder.CreateIndex(
                name: "IX_PickupTimeSlots_SlotDate_StartTime",
                table: "PickupTimeSlots",
                columns: new[] { "SlotDate", "StartTime" },
                unique: true);
        }
    }
}
