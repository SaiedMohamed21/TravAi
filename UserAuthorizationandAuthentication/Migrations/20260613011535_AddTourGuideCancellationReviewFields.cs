using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravAi.Migrations
{
    /// <inheritdoc />
    public partial class AddTourGuideCancellationReviewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "tourguide_TourBookings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReviewNotes",
                table: "tourguide_TourBookings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReviewStatus",
                table: "tourguide_TourBookings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CancellationReviewedByAdminUserId",
                table: "tourguide_TourBookings",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelledByRole",
                table: "tourguide_TourBookings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "tourguide_TourBookings");

            migrationBuilder.DropColumn(
                name: "CancellationReviewNotes",
                table: "tourguide_TourBookings");

            migrationBuilder.DropColumn(
                name: "CancellationReviewStatus",
                table: "tourguide_TourBookings");

            migrationBuilder.DropColumn(
                name: "CancellationReviewedByAdminUserId",
                table: "tourguide_TourBookings");

            migrationBuilder.DropColumn(
                name: "CancelledByRole",
                table: "tourguide_TourBookings");
        }
    }
}
