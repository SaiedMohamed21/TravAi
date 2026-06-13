using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravAi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateComplaintSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AirlineBookingId",
                table: "hotel_Complaints",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TourBookingId",
                table: "hotel_Complaints",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_hotel_Complaints_AirlineBookingId",
                table: "hotel_Complaints",
                column: "AirlineBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_hotel_Complaints_TourBookingId",
                table: "hotel_Complaints",
                column: "TourBookingId");

            migrationBuilder.AddForeignKey(
                name: "FK_hotel_Complaints_airline_Bookings_AirlineBookingId",
                table: "hotel_Complaints",
                column: "AirlineBookingId",
                principalTable: "airline_Bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_hotel_Complaints_tourguide_TourBookings_TourBookingId",
                table: "hotel_Complaints",
                column: "TourBookingId",
                principalTable: "tourguide_TourBookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Safe Enum Migration for ComplaintType
            migrationBuilder.Sql("UPDATE hotel_Complaints SET ComplaintType = 'Hotel' WHERE ComplaintType = 'Booking';");
            migrationBuilder.Sql("UPDATE hotel_Complaints SET ComplaintType = 'Service' WHERE ComplaintType = 'Platform';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_hotel_Complaints_airline_Bookings_AirlineBookingId",
                table: "hotel_Complaints");

            migrationBuilder.DropForeignKey(
                name: "FK_hotel_Complaints_tourguide_TourBookings_TourBookingId",
                table: "hotel_Complaints");

            migrationBuilder.DropIndex(
                name: "IX_hotel_Complaints_AirlineBookingId",
                table: "hotel_Complaints");

            migrationBuilder.DropIndex(
                name: "IX_hotel_Complaints_TourBookingId",
                table: "hotel_Complaints");

            migrationBuilder.DropColumn(
                name: "AirlineBookingId",
                table: "hotel_Complaints");

            migrationBuilder.DropColumn(
                name: "TourBookingId",
                table: "hotel_Complaints");

            // Revert Enum Migration for ComplaintType
            migrationBuilder.Sql("UPDATE hotel_Complaints SET ComplaintType = 'Booking' WHERE ComplaintType = 'Hotel';");
            migrationBuilder.Sql("UPDATE hotel_Complaints SET ComplaintType = 'Platform' WHERE ComplaintType = 'Service';");
        }
    }
}
