using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravAi.Migrations
{
    [DbContext(typeof(TravAi.Data.ApplicationDbContext))]
    [Migration("20260610001000_AddSimpleStripeCheckoutAndTransactions_ManualPaymentOnly")]
    public partial class AddSimpleStripeCheckoutAndTransactions_ManualPaymentOnly : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create CheckoutSessions
            migrationBuilder.CreateTable(
                name: "CheckoutSessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    CheckoutType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "usd"),
                    StripeCheckoutSessionId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    StripePaymentIntentId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckoutSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CheckoutSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // 2. Create CheckoutSessionItems
            migrationBuilder.CreateTable(
                name: "CheckoutSessionItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CheckoutSessionId = table.Column<long>(type: "bigint", nullable: false),
                    ItemType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReferenceId = table.Column<long>(type: "bigint", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckoutSessionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CheckoutSessionItems_CheckoutSessions_CheckoutSessionId",
                        column: x => x.CheckoutSessionId,
                        principalTable: "CheckoutSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // 3. Create PaymentTransactions
            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CheckoutSessionId = table.Column<long>(type: "bigint", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Stripe"),
                    ProviderTransactionId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ProviderCheckoutSessionId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RawProviderResponse = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_CheckoutSessions_CheckoutSessionId",
                        column: x => x.CheckoutSessionId,
                        principalTable: "CheckoutSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // 4. Create StripeWebhookEvents
            migrationBuilder.CreateTable(
                name: "StripeWebhookEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StripeEventId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CheckoutSessionId = table.Column<long>(type: "bigint", nullable: true),
                    PaymentTransactionId = table.Column<long>(type: "bigint", nullable: true),
                    RawJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StripeWebhookEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StripeWebhookEvents_CheckoutSessions_CheckoutSessionId",
                        column: x => x.CheckoutSessionId,
                        principalTable: "CheckoutSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StripeWebhookEvents_PaymentTransactions_PaymentTransactionId",
                        column: x => x.PaymentTransactionId,
                        principalTable: "PaymentTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // 5. Indexes
            migrationBuilder.CreateIndex(
                name: "IX_CheckoutSessions_UserId",
                table: "CheckoutSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckoutSessionItems_CheckoutSessionId",
                table: "CheckoutSessionItems",
                column: "CheckoutSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_CheckoutSessionId",
                table: "PaymentTransactions",
                column: "CheckoutSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_StripeWebhookEvents_CheckoutSessionId",
                table: "StripeWebhookEvents",
                column: "CheckoutSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_StripeWebhookEvents_PaymentTransactionId",
                table: "StripeWebhookEvents",
                column: "PaymentTransactionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "StripeWebhookEvents");
            migrationBuilder.DropTable(name: "CheckoutSessionItems");
            migrationBuilder.DropTable(name: "PaymentTransactions");
            migrationBuilder.DropTable(name: "CheckoutSessions");
        }
    }
}
