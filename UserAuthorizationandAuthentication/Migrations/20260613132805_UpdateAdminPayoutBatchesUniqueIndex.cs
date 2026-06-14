using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravAi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAdminPayoutBatchesUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_admin_PayoutBatches_ProviderType_ProviderId_WeekStartDate",
                table: "admin_PayoutBatches");

            migrationBuilder.CreateIndex(
                name: "IX_admin_PayoutBatches_ProviderType_ProviderId_WeekStartDate_WeekEndDate_Currency",
                table: "admin_PayoutBatches",
                columns: new[] { "ProviderType", "ProviderId", "WeekStartDate", "WeekEndDate", "Currency" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_admin_PayoutBatches_ProviderType_ProviderId_WeekStartDate_WeekEndDate_Currency",
                table: "admin_PayoutBatches");

            migrationBuilder.CreateIndex(
                name: "IX_admin_PayoutBatches_ProviderType_ProviderId_WeekStartDate",
                table: "admin_PayoutBatches",
                columns: new[] { "ProviderType", "ProviderId", "WeekStartDate" });
        }
    }
}
