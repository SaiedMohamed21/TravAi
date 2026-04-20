using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserAuthorizationandAuthentication.Migrations
{
    /// <inheritdoc />
    public partial class AddFlightSegmentsAndLayovers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "airline_FlightLayovers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FlightId = table.Column<long>(type: "bigint", nullable: false),
                    LayoverOrder = table.Column<int>(type: "int", nullable: false),
                    AirportName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DurationString = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airline_FlightLayovers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_airline_FlightLayovers_airline_Flights_FlightId",
                        column: x => x.FlightId,
                        principalTable: "airline_Flights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "airline_FlightSegments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FlightId = table.Column<long>(type: "bigint", nullable: false),
                    SegmentNumber = table.Column<int>(type: "int", nullable: false),
                    Amenities = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LegroomInches = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airline_FlightSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_airline_FlightSegments_airline_Flights_FlightId",
                        column: x => x.FlightId,
                        principalTable: "airline_Flights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_airline_FlightLayovers_FlightId",
                table: "airline_FlightLayovers",
                column: "FlightId");

            migrationBuilder.CreateIndex(
                name: "IX_airline_FlightSegments_FlightId",
                table: "airline_FlightSegments",
                column: "FlightId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "airline_FlightLayovers");

            migrationBuilder.DropTable(
                name: "airline_FlightSegments");
        }
    }
}
