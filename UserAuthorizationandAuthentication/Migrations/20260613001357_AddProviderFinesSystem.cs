using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravAi.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderFinesSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_ProviderFines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProviderType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderId = table.Column<long>(type: "bigint", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ComplaintId = table.Column<long>(type: "bigint", nullable: true),
                    HotelBookingId = table.Column<long>(type: "bigint", nullable: true),
                    TourBookingId = table.Column<long>(type: "bigint", nullable: true),
                    AirlineBookingId = table.Column<long>(type: "bigint", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AdminNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedByAdminUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CancelledByAdminUserId = table.Column<long>(type: "bigint", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_ProviderFines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_admin_ProviderFines_Users_CancelledByAdminUserId",
                        column: x => x.CancelledByAdminUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_admin_ProviderFines_Users_CreatedByAdminUserId",
                        column: x => x.CreatedByAdminUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_admin_ProviderFines_hotel_Complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "hotel_Complaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_ProviderFines_CancelledByAdminUserId",
                table: "admin_ProviderFines",
                column: "CancelledByAdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_admin_ProviderFines_ComplaintId",
                table: "admin_ProviderFines",
                column: "ComplaintId");

            migrationBuilder.CreateIndex(
                name: "IX_admin_ProviderFines_CreatedAt",
                table: "admin_ProviderFines",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_admin_ProviderFines_CreatedByAdminUserId",
                table: "admin_ProviderFines",
                column: "CreatedByAdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_admin_ProviderFines_ProviderType_ProviderId",
                table: "admin_ProviderFines",
                columns: new[] { "ProviderType", "ProviderId" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_ProviderFines_Status",
                table: "admin_ProviderFines",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_admin_ProviderFines_TourBookingId",
                table: "admin_ProviderFines",
                column: "TourBookingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_ProviderFines");
        }
    }
}
