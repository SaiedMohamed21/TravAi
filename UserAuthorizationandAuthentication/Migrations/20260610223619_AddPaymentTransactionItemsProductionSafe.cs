using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravAi.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTransactionItemsProductionSafe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "PaymentTransactions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripePaymentIntentId",
                table: "PaymentTransactions",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeSessionId",
                table: "PaymentTransactions",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "PaymentTransactions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "PaymentTransactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "PaymentTransactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PaymentTransactionItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentTransactionId = table.Column<long>(type: "bigint", nullable: false),
                    BookingType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BookingId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentTransactionItems_PaymentTransactions_PaymentTransactionId",
                        column: x => x.PaymentTransactionId,
                        principalTable: "PaymentTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_UserId",
                table: "PaymentTransactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactionItems_PaymentTransactionId",
                table: "PaymentTransactionItems",
                column: "PaymentTransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentTransactions_Users_UserId",
                table: "PaymentTransactions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentTransactions_Users_UserId",
                table: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "PaymentTransactionItems");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_UserId",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "StripePaymentIntentId",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "StripeSessionId",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "PaymentTransactions");
        }
    }
}
