using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravAi.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderStripePayoutPaymentTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_ProviderStripePayoutAccounts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProviderType = table.Column<int>(type: "int", nullable: false),
                    ProviderId = table.Column<long>(type: "bigint", nullable: false),
                    ProviderNameSnapshot = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ProviderPayoutAccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StripeConnectedAccountId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StripeAccountDisplayName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    BankLast4 = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_ProviderStripePayoutAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "admin_PayoutStripePayments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayoutBatchId = table.Column<long>(type: "bigint", nullable: false),
                    ProviderStripePayoutAccountId = table.Column<long>(type: "bigint", nullable: false),
                    ProviderType = table.Column<int>(type: "int", nullable: false),
                    ProviderId = table.Column<long>(type: "bigint", nullable: false),
                    StripeConnectedAccountId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StripeCheckoutSessionId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StripePaymentIntentId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByAdminUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_PayoutStripePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_admin_PayoutStripePayments_admin_PayoutBatches_PayoutBatchId",
                        column: x => x.PayoutBatchId,
                        principalTable: "admin_PayoutBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_admin_PayoutStripePayments_admin_ProviderStripePayoutAccounts_ProviderStripePayoutAccountId",
                        column: x => x.ProviderStripePayoutAccountId,
                        principalTable: "admin_ProviderStripePayoutAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_PayoutStripePayments_PayoutBatchId",
                table: "admin_PayoutStripePayments",
                column: "PayoutBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_admin_PayoutStripePayments_ProviderStripePayoutAccountId",
                table: "admin_PayoutStripePayments",
                column: "ProviderStripePayoutAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_admin_ProviderStripePayoutAccounts_ProviderType_ProviderId",
                table: "admin_ProviderStripePayoutAccounts",
                columns: new[] { "ProviderType", "ProviderId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_PayoutStripePayments");

            migrationBuilder.DropTable(
                name: "admin_ProviderStripePayoutAccounts");
        }
    }
}
