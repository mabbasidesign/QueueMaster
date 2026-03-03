using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OrderService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Orders",
                columns: new[] { "Id", "CreatedAtUtc", "CustomerName", "ProductName", "Quantity", "Status", "UnitPrice" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 2, 26, 12, 25, 1, 899, DateTimeKind.Utc).AddTicks(6884), "John Smith", "Laptop", 1, "Completed", 999.99m },
                    { 2, new DateTime(2026, 3, 1, 12, 25, 1, 899, DateTimeKind.Utc).AddTicks(7616), "Jane Doe", "Monitor", 2, "Processing", 299.99m },
                    { 3, new DateTime(2026, 3, 3, 12, 25, 1, 899, DateTimeKind.Utc).AddTicks(7629), "Bob Johnson", "Keyboard", 3, "Created", 79.99m },
                    { 4, new DateTime(2026, 2, 21, 12, 25, 1, 899, DateTimeKind.Utc).AddTicks(7632), "Alice Brown", "Mouse", 5, "Completed", 29.99m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerName",
                table: "Orders",
                column: "CustomerName");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                table: "Orders",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Orders");
        }
    }
}
