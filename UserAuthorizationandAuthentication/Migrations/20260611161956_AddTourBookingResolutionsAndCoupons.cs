using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravAi.Migrations
{
    /// <inheritdoc />
    public partial class AddTourBookingResolutionsAndCoupons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tourguide_TourBookingResolutions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginalBookingId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    ResolutionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NewBookingId = table.Column<long>(type: "bigint", nullable: true),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tourguide_TourBookingResolutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tourguide_TourBookingResolutions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tourguide_TourBookingResolutions_tourguide_TourBookings_NewBookingId",
                        column: x => x.NewBookingId,
                        principalTable: "tourguide_TourBookings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tourguide_TourBookingResolutions_tourguide_TourBookings_OriginalBookingId",
                        column: x => x.OriginalBookingId,
                        principalTable: "tourguide_TourBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tourguide_UserTourCompensationCoupons",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    TriggeringBookingId = table.Column<long>(type: "bigint", nullable: false),
                    CouponCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tourguide_UserTourCompensationCoupons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tourguide_UserTourCompensationCoupons_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tourguide_UserTourCompensationCoupons_tourguide_TourBookings_TriggeringBookingId",
                        column: x => x.TriggeringBookingId,
                        principalTable: "tourguide_TourBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_TourBookingResolutions_NewBookingId",
                table: "tourguide_TourBookingResolutions",
                column: "NewBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_TourBookingResolutions_OriginalBookingId",
                table: "tourguide_TourBookingResolutions",
                column: "OriginalBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_TourBookingResolutions_UserId",
                table: "tourguide_TourBookingResolutions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_UserTourCompensationCoupons_TriggeringBookingId",
                table: "tourguide_UserTourCompensationCoupons",
                column: "TriggeringBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_UserTourCompensationCoupons_UserId",
                table: "tourguide_UserTourCompensationCoupons",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tourguide_TourBookingResolutions");

            migrationBuilder.DropTable(
                name: "tourguide_UserTourCompensationCoupons");
        }
    }
}
