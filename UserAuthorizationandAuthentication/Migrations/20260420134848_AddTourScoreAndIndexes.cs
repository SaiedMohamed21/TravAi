using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserAuthorizationandAuthentication.Migrations
{
    /// <inheritdoc />
    public partial class AddTourScoreAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TourType",
                table: "tourguide_Tours",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "tourguide_Tours",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TourScore",
                table: "tourguide_Tours",
                type: "decimal(5,4)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_Tours_AvailableDateTime",
                table: "tourguide_Tours",
                column: "AvailableDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_Tours_BasePriceUsd",
                table: "tourguide_Tours",
                column: "BasePriceUsd");

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_Tours_City",
                table: "tourguide_Tours",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_Tours_City_TourScore",
                table: "tourguide_Tours",
                columns: new[] { "City", "TourScore" });

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_Tours_Rating",
                table: "tourguide_Tours",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_Tours_TourScore",
                table: "tourguide_Tours",
                column: "TourScore",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_Tours_TourType",
                table: "tourguide_Tours",
                column: "TourType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tourguide_Tours_AvailableDateTime",
                table: "tourguide_Tours");

            migrationBuilder.DropIndex(
                name: "IX_tourguide_Tours_BasePriceUsd",
                table: "tourguide_Tours");

            migrationBuilder.DropIndex(
                name: "IX_tourguide_Tours_City",
                table: "tourguide_Tours");

            migrationBuilder.DropIndex(
                name: "IX_tourguide_Tours_City_TourScore",
                table: "tourguide_Tours");

            migrationBuilder.DropIndex(
                name: "IX_tourguide_Tours_Rating",
                table: "tourguide_Tours");

            migrationBuilder.DropIndex(
                name: "IX_tourguide_Tours_TourScore",
                table: "tourguide_Tours");

            migrationBuilder.DropIndex(
                name: "IX_tourguide_Tours_TourType",
                table: "tourguide_Tours");

            migrationBuilder.DropColumn(
                name: "TourScore",
                table: "tourguide_Tours");

            migrationBuilder.AlterColumn<string>(
                name: "TourType",
                table: "tourguide_Tours",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "tourguide_Tours",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
