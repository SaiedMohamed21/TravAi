using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravAi.Migrations
{
    /// <inheritdoc />
    public partial class AddPassengerDetailsStatusToAirlineBookings : Migration
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

            migrationBuilder.AddColumn<DateTime>(
                name: "PassengerDetailsCompletedAt",
                table: "airline_Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PassengerDetailsStatus",
                table: "airline_Bookings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Incomplete");

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

            migrationBuilder.DropColumn(
                name: "PassengerDetailsCompletedAt",
                table: "airline_Bookings");

            migrationBuilder.DropColumn(
                name: "PassengerDetailsStatus",
                table: "airline_Bookings");

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
