using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravAi.Migrations
{
    /// <inheritdoc />
    public partial class AddTourReviewsNavigationExplicit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_airline_Flights_airline_Airlines_AirlineId",
                table: "airline_Flights");

            // Columns already exist in the database manually
            // migrationBuilder.AddColumn<DateTime>(
            //     name: "ArrivalTime",
            //     table: "airline_FlightSegments",
            //     type: "datetime2",
            //     nullable: true);

            // migrationBuilder.AddColumn<DateTime>(
            //     name: "DepartureTime",
            //     table: "airline_FlightSegments",
            //     type: "datetime2",
            //     nullable: true);

            // migrationBuilder.AddColumn<string>(
            //     name: "FromAirportCode",
            //     table: "airline_FlightSegments",
            //     type: "nvarchar(3)",
            //     nullable: true);

            // migrationBuilder.AddColumn<string>(
            //     name: "ToAirportCode",
            //     table: "airline_FlightSegments",
            //     type: "nvarchar(3)",
            //     nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "airline_Flights",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<int>(
                name: "NumberOfStops",
                table: "airline_Flights",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DepartureTime",
                table: "airline_Flights",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "DepartureAirportCode",
                table: "airline_Flights",
                type: "nvarchar(3)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(3)");

            migrationBuilder.AlterColumn<int>(
                name: "AvailableSeats",
                table: "airline_Flights",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ArrivalTime",
                table: "airline_Flights",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "ArrivalAirportCode",
                table: "airline_Flights",
                type: "nvarchar(3)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(3)");

            migrationBuilder.AlterColumn<long>(
                name: "AirlineId",
                table: "airline_Flights",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            // migrationBuilder.AddColumn<string>(
            //     name: "Currency",
            //     table: "airline_Flights",
            //     type: "nvarchar(max)",
            //     nullable: false,
            //     defaultValue: "");

            // migrationBuilder.AddColumn<string>(
            //     name: "Duration",
            //     table: "airline_Flights",
            //     type: "nvarchar(max)",
            //     nullable: true);

            // migrationBuilder.AddColumn<int>(
            //     name: "DurationMinutes",
            //     table: "airline_Flights",
            //     type: "int",
            //     nullable: true);

            // migrationBuilder.CreateIndex(
            //     name: "IX_airline_FlightSegments_FromAirportCode",
            //     table: "airline_FlightSegments",
            //     column: "FromAirportCode");

            // migrationBuilder.CreateIndex(
            //     name: "IX_airline_FlightSegments_ToAirportCode",
            //     table: "airline_FlightSegments",
            //     column: "ToAirportCode");

            migrationBuilder.AddForeignKey(
                name: "FK_airline_Flights_airline_Airlines_AirlineId",
                table: "airline_Flights",
                column: "AirlineId",
                principalTable: "airline_Airlines",
                principalColumn: "Id");

            // migrationBuilder.AddForeignKey(
            //     name: "FK_airline_FlightSegments_airline_Airports_FromAirportCode",
            //     table: "airline_FlightSegments",
            //     column: "FromAirportCode",
            //     principalTable: "airline_Airports",
            //     principalColumn: "Code");

            // migrationBuilder.AddForeignKey(
            //     name: "FK_airline_FlightSegments_airline_Airports_ToAirportCode",
            //     table: "airline_FlightSegments",
            //     column: "ToAirportCode",
            //     principalTable: "airline_Airports",
            //     principalColumn: "Code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_airline_Flights_airline_Airlines_AirlineId",
                table: "airline_Flights");

            migrationBuilder.DropForeignKey(
                name: "FK_airline_FlightSegments_airline_Airports_FromAirportCode",
                table: "airline_FlightSegments");

            migrationBuilder.DropForeignKey(
                name: "FK_airline_FlightSegments_airline_Airports_ToAirportCode",
                table: "airline_FlightSegments");

            migrationBuilder.DropIndex(
                name: "IX_airline_FlightSegments_FromAirportCode",
                table: "airline_FlightSegments");

            migrationBuilder.DropIndex(
                name: "IX_airline_FlightSegments_ToAirportCode",
                table: "airline_FlightSegments");

            migrationBuilder.DropColumn(
                name: "ArrivalTime",
                table: "airline_FlightSegments");

            migrationBuilder.DropColumn(
                name: "DepartureTime",
                table: "airline_FlightSegments");

            migrationBuilder.DropColumn(
                name: "FromAirportCode",
                table: "airline_FlightSegments");

            migrationBuilder.DropColumn(
                name: "ToAirportCode",
                table: "airline_FlightSegments");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "airline_Flights");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "airline_Flights");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                table: "airline_Flights");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "airline_Flights",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "NumberOfStops",
                table: "airline_Flights",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DepartureTime",
                table: "airline_Flights",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DepartureAirportCode",
                table: "airline_Flights",
                type: "nvarchar(3)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(3)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AvailableSeats",
                table: "airline_Flights",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ArrivalTime",
                table: "airline_Flights",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ArrivalAirportCode",
                table: "airline_Flights",
                type: "nvarchar(3)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(3)",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "AirlineId",
                table: "airline_Flights",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_airline_Flights_airline_Airlines_AirlineId",
                table: "airline_Flights",
                column: "AirlineId",
                principalTable: "airline_Airlines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
