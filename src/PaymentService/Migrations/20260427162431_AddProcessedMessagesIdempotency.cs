using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentService.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessedMessagesIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcessedMessages",
                columns: table => new
                {
                    MessageId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedMessages", x => x.MessageId);
                });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "TransactionId",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 22, 16, 24, 30, 674, DateTimeKind.Utc).AddTicks(6133));

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "TransactionId",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 25, 16, 24, 30, 674, DateTimeKind.Utc).AddTicks(6806));

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "TransactionId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 27, 16, 24, 30, 674, DateTimeKind.Utc).AddTicks(6822));

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "TransactionId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 17, 16, 24, 30, 674, DateTimeKind.Utc).AddTicks(6827));

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedMessages_OrderId",
                table: "ProcessedMessages",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedMessages");

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "TransactionId",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 2, 26, 12, 26, 2, 452, DateTimeKind.Utc).AddTicks(5967));

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "TransactionId",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 3, 1, 12, 26, 2, 452, DateTimeKind.Utc).AddTicks(6349));

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "TransactionId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 3, 3, 12, 26, 2, 452, DateTimeKind.Utc).AddTicks(6357));

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "TransactionId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 2, 21, 12, 26, 2, 452, DateTimeKind.Utc).AddTicks(6360));
        }
    }
}
