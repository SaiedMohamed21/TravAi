using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravAi.Migrations
{
    /// <inheritdoc />
    public partial class AddAIChatHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StripeWebhookEvents_CheckoutSessions_CheckoutSessionId",
                table: "StripeWebhookEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_StripeWebhookEvents_PaymentTransactions_PaymentTransactionId",
                table: "StripeWebhookEvents");

            migrationBuilder.CreateTable(
                name: "ai_ChatSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_ChatSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_ChatSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ai_ChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChatSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_ChatMessages_ai_ChatSessions_ChatSessionId",
                        column: x => x.ChatSessionId,
                        principalTable: "ai_ChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_ChatMessages_ChatSessionId",
                table: "ai_ChatMessages",
                column: "ChatSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_ChatSessions_UserId",
                table: "ai_ChatSessions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_StripeWebhookEvents_CheckoutSessions_CheckoutSessionId",
                table: "StripeWebhookEvents",
                column: "CheckoutSessionId",
                principalTable: "CheckoutSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StripeWebhookEvents_PaymentTransactions_PaymentTransactionId",
                table: "StripeWebhookEvents",
                column: "PaymentTransactionId",
                principalTable: "PaymentTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StripeWebhookEvents_CheckoutSessions_CheckoutSessionId",
                table: "StripeWebhookEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_StripeWebhookEvents_PaymentTransactions_PaymentTransactionId",
                table: "StripeWebhookEvents");

            migrationBuilder.DropTable(
                name: "ai_ChatMessages");

            migrationBuilder.DropTable(
                name: "ai_ChatSessions");

            migrationBuilder.AddForeignKey(
                name: "FK_StripeWebhookEvents_CheckoutSessions_CheckoutSessionId",
                table: "StripeWebhookEvents",
                column: "CheckoutSessionId",
                principalTable: "CheckoutSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_StripeWebhookEvents_PaymentTransactions_PaymentTransactionId",
                table: "StripeWebhookEvents",
                column: "PaymentTransactionId",
                principalTable: "PaymentTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
