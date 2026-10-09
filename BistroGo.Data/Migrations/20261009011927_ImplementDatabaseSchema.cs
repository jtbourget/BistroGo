using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BistroGo.Data.Migrations
{
    /// <inheritdoc />
    public partial class ImplementDatabaseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // --- Custom: backfill CreatedAt for users that existed before this migration ---
            // SQLite can't ADD a column with a CURRENT_TIMESTAMP default, so the column above
            // gets the constant 0001-01-01. Give existing users "now" instead of year 1.
            // (Their real sign-up date was never recorded, so migration time is the best we have.)
            migrationBuilder.Sql(
                "UPDATE \"AspNetUsers\" SET \"CreatedAt\" = CURRENT_TIMESTAMP " +
                "WHERE \"CreatedAt\" = '0001-01-01 00:00:00';");

            migrationBuilder.CreateTable(
                name: "MenuCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PickupTimeSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SlotDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    MaxOrders = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 10)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickupTimeSlots", x => x.Id);
                    table.CheckConstraint("CK_PickupTimeSlots_EndAfterStart", "\"EndTime\" > \"StartTime\"");
                    table.CheckConstraint("CK_PickupTimeSlots_MaxOrders_Positive", "\"MaxOrders\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "MenuItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Price = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    IsAvailable = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItems", x => x.Id);
                    table.CheckConstraint("CK_MenuItems_Price_NonNegative", "CAST(\"Price\" AS REAL) >= 0");
                    table.ForeignKey(
                        name: "FK_MenuItems_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MenuItems_MenuCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "MenuCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    GuestName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    GuestPhone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    GuestEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    GuestToken = table.Column<Guid>(type: "TEXT", nullable: true),
                    PickupSlotId = table.Column<int>(type: "INTEGER", nullable: true),
                    StatusId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsFinalized = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    Subtotal = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false, defaultValue: 0m),
                    Tax = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false, defaultValue: 0m),
                    Total = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false, defaultValue: 0m),
                    ConfirmationCode = table.Column<string>(type: "TEXT", maxLength: 12, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    FinalizedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    ArchivedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.CheckConstraint("CK_Orders_Amounts_NonNegative", "CAST(\"Subtotal\" AS REAL) >= 0 AND CAST(\"Tax\" AS REAL) >= 0 AND CAST(\"Total\" AS REAL) >= 0");
                    table.CheckConstraint("CK_Orders_ArchivedConsistent", "(\"IsArchived\" = 0 AND \"ArchivedAt\" IS NULL) OR (\"IsArchived\" = 1 AND \"ArchivedAt\" IS NOT NULL AND \"IsFinalized\" = 1)");
                    table.CheckConstraint("CK_Orders_FinalizedConsistent", "(\"IsFinalized\" = 0 AND \"FinalizedAt\" IS NULL AND \"StatusId\" IS NULL AND \"ConfirmationCode\" IS NULL) OR (\"IsFinalized\" = 1 AND \"FinalizedAt\" IS NOT NULL AND \"StatusId\" IS NOT NULL AND \"ConfirmationCode\" IS NOT NULL AND \"PickupSlotId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Orders_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Orders_OrderStatuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "OrderStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_PickupTimeSlots_PickupSlotId",
                        column: x => x.PickupSlotId,
                        principalTable: "PickupTimeSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Expenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RecordedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    MenuItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ExpenseDate = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => x.Id);
                    table.CheckConstraint("CK_Expenses_Amount_NonNegative", "CAST(\"Amount\" AS REAL) >= 0");
                    table.ForeignKey(
                        name: "FK_Expenses_AspNetUsers_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Expenses_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    MenuItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    UnitPrice = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    SpecialInstructions = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.CheckConstraint("CK_OrderItems_Quantity_Positive", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_OrderItems_UnitPrice_NonNegative", "CAST(\"UnitPrice\" AS REAL) >= 0");
                    table.ForeignKey(
                        name: "FK_OrderItems_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ratings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    Stars = table.Column<int>(type: "INTEGER", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ratings", x => x.Id);
                    table.CheckConstraint("CK_Ratings_Stars_Range", "\"Stars\" BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_Ratings_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "MenuCategories",
                columns: new[] { "Id", "DisplayOrder", "Name" },
                values: new object[,]
                {
                    { 1, 1, "Appetizer" },
                    { 2, 2, "Entree" },
                    { 3, 3, "Dessert" },
                    { 4, 4, "Drink" }
                });

            migrationBuilder.InsertData(
                table: "OrderStatuses",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Received" },
                    { 2, "Preparing" },
                    { 3, "Ready" },
                    { 4, "PickedUp" },
                    { 5, "Cancelled" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_MenuItemId",
                table: "Expenses",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_RecordedByUserId",
                table: "Expenses",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuCategories_Name",
                table: "MenuCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_CategoryId",
                table: "MenuItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_CreatedByUserId",
                table: "MenuItems",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_MenuItemId",
                table: "OrderItems",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ConfirmationCode",
                table: "Orders",
                column: "ConfirmationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_GuestToken",
                table: "Orders",
                column: "GuestToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PickupSlotId",
                table: "Orders",
                column: "PickupSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_StatusId",
                table: "Orders",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId_IsFinalized",
                table: "Orders",
                columns: new[] { "UserId", "IsFinalized" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatuses_Name",
                table: "OrderStatuses",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PickupTimeSlots_SlotDate_StartTime",
                table: "PickupTimeSlots",
                columns: new[] { "SlotDate", "StartTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_OrderId",
                table: "Ratings",
                column: "OrderId");

            // --- Custom: rename the old "Guest" role to "Customer" ---
            // Only matters for databases where the app ran before the rename (roles are
            // created at startup by Program.cs). Fresh databases have no roles yet, so
            // all three statements simply match zero rows.

            // 1. If BOTH roles exist, give every Guest user the Customer role too...
            migrationBuilder.Sql(@"
                INSERT OR IGNORE INTO ""AspNetUserRoles"" (""UserId"", ""RoleId"")
                SELECT ur.""UserId"", c.""Id""
                FROM ""AspNetUserRoles"" ur
                JOIN ""AspNetRoles"" g ON g.""Id"" = ur.""RoleId"" AND g.""NormalizedName"" = 'GUEST'
                JOIN ""AspNetRoles"" c ON c.""NormalizedName"" = 'CUSTOMER';");

            // 2. ...then delete Guest (its AspNetUserRoles rows cascade away).
            migrationBuilder.Sql(@"
                DELETE FROM ""AspNetRoles""
                WHERE ""NormalizedName"" = 'GUEST'
                  AND EXISTS (SELECT 1 FROM ""AspNetRoles"" WHERE ""NormalizedName"" = 'CUSTOMER');");

            // 3. If only Guest exists, rename it in place - same Id, so every user link survives.
            migrationBuilder.Sql(@"
                UPDATE ""AspNetRoles""
                SET ""Name"" = 'Customer',
                    ""NormalizedName"" = 'CUSTOMER',
                    ""ConcurrencyStamp"" = lower(hex(randomblob(16)))
                WHERE ""NormalizedName"" = 'GUEST';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Note: the Guest -> Customer role rename is intentionally NOT reversed.
            // The code only knows "Customer" now; turning it back into "Guest" would leave
            // Customers without a role the app recognizes. It's a one-way data fix.

            migrationBuilder.DropTable(
                name: "Expenses");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "Ratings");

            migrationBuilder.DropTable(
                name: "MenuItems");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "MenuCategories");

            migrationBuilder.DropTable(
                name: "OrderStatuses");

            migrationBuilder.DropTable(
                name: "PickupTimeSlots");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AspNetUsers");
        }
    }
}
