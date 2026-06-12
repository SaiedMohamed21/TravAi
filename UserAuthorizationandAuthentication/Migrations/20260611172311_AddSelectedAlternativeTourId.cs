using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravAi.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectedAlternativeTourId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SelectedAlternativeTourId",
                table: "tourguide_TourBookingResolutions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_TourBookingResolutions_SelectedAlternativeTourId",
                table: "tourguide_TourBookingResolutions",
                column: "SelectedAlternativeTourId");

            migrationBuilder.AddForeignKey(
                name: "FK_tourguide_TourBookingResolutions_tourguide_Tours_SelectedAlternativeTourId",
                table: "tourguide_TourBookingResolutions",
                column: "SelectedAlternativeTourId",
                principalTable: "tourguide_Tours",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tourguide_TourBookingResolutions_tourguide_Tours_SelectedAlternativeTourId",
                table: "tourguide_TourBookingResolutions");

            migrationBuilder.DropIndex(
                name: "IX_tourguide_TourBookingResolutions_SelectedAlternativeTourId",
                table: "tourguide_TourBookingResolutions");

            migrationBuilder.DropColumn(
                name: "SelectedAlternativeTourId",
                table: "tourguide_TourBookingResolutions");
        }
    }
}
