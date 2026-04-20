using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserAuthorizationandAuthentication.Migrations
{
    /// <inheritdoc />
    public partial class AddFlightClass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FlightClass",
                table: "airline_Flights",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FlightClass",
                table: "airline_Flights");
        }
    }
}
