using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PaymentService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    TransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Method = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.TransactionId);
                });

            migrationBuilder.InsertData(
                table: "Payments",
                columns: new[] { "TransactionId", "Amount", "CreatedAtUtc", "Currency", "Method", "OrderId", "Status" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), 999.99m, new DateTime(2026, 2, 26, 12, 26, 2, 452, DateTimeKind.Utc).AddTicks(5967), "USD", "Card", 1, "Completed" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), 599.98m, new DateTime(2026, 3, 1, 12, 26, 2, 452, DateTimeKind.Utc).AddTicks(6349), "USD", "BankTransfer", 2, "Completed" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), 239.97m, new DateTime(2026, 3, 3, 12, 26, 2, 452, DateTimeKind.Utc).AddTicks(6357), "USD", "Card", 3, "Processing" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), 149.95m, new DateTime(2026, 2, 21, 12, 26, 2, 452, DateTimeKind.Utc).AddTicks(6360), "USD", "Wallet", 4, "Completed" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CreatedAtUtc",
                table: "Payments",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrderId",
                table: "Payments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status",
                table: "Payments",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payments");
        }
    }
}
