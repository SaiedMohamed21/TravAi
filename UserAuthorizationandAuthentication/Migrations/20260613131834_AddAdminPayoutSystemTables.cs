using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravAi.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminPayoutSystemTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_PayoutBatches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProviderType = table.Column<int>(type: "int", nullable: false),
                    ProviderId = table.Column<long>(type: "bigint", nullable: false),
                    ProviderNameSnapshot = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    WeekStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WeekEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalRefundAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetAfterRefundAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCommissionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalFineDeductionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FinalPayoutAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneratedByAdminUserId = table.Column<long>(type: "bigint", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmedByAdminUserId = table.Column<long>(type: "bigint", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_PayoutBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_admin_PayoutBatches_Users_ConfirmedByAdminUserId",
                        column: x => x.ConfirmedByAdminUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_admin_PayoutBatches_Users_GeneratedByAdminUserId",
                        column: x => x.GeneratedByAdminUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "admin_PayoutFineDeductions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayoutBatchId = table.Column<long>(type: "bigint", nullable: false),
                    ProviderFineId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReasonSnapshot = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FineCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_PayoutFineDeductions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_admin_PayoutFineDeductions_admin_PayoutBatches_PayoutBatchId",
                        column: x => x.PayoutBatchId,
                        principalTable: "admin_PayoutBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_admin_PayoutFineDeductions_admin_ProviderFines_ProviderFineId",
                        column: x => x.ProviderFineId,
                        principalTable: "admin_ProviderFines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "admin_PayoutItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayoutBatchId = table.Column<long>(type: "bigint", nullable: false),
                    BookingType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BookingId = table.Column<long>(type: "bigint", nullable: false),
                    PaymentTransactionId = table.Column<long>(type: "bigint", nullable: true),
                    PaymentTransactionItemId = table.Column<long>(type: "bigint", nullable: true),
                    ServiceEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OriginalPaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetAfterRefundAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RefundReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CommissionPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProviderAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_PayoutItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_admin_PayoutItems_admin_PayoutBatches_PayoutBatchId",
                        column: x => x.PayoutBatchId,
                        principalTable: "admin_PayoutBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_PayoutBatches_ConfirmedByAdminUserId",
                table: "admin_PayoutBatches",
                column: "ConfirmedByAdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_admin_PayoutBatches_GeneratedByAdminUserId",
                table: "admin_PayoutBatches",
                column: "GeneratedByAdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_admin_PayoutBatches_ProviderType_ProviderId_WeekStartDate",
                table: "admin_PayoutBatches",
                columns: new[] { "ProviderType", "ProviderId", "WeekStartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_PayoutFineDeductions_PayoutBatchId",
                table: "admin_PayoutFineDeductions",
                column: "PayoutBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_admin_PayoutFineDeductions_ProviderFineId",
                table: "admin_PayoutFineDeductions",
                column: "ProviderFineId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_admin_PayoutItems_BookingType_BookingId",
                table: "admin_PayoutItems",
                columns: new[] { "BookingType", "BookingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_admin_PayoutItems_PayoutBatchId",
                table: "admin_PayoutItems",
                column: "PayoutBatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_PayoutFineDeductions");

            migrationBuilder.DropTable(
                name: "admin_PayoutItems");

            migrationBuilder.DropTable(
                name: "admin_PayoutBatches");
        }
    }
}
