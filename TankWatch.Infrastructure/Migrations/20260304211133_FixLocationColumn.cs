using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace TankWatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixLocationColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Point>(
                name: "Location",
                table: "GasStations",
                type: "geography",
                nullable: true,
                computedColumnSql: "ST_SetSRID(ST_MakePoint(\"Longitude\", \"Latitude\"), 4326)",
                stored: true,
                oldClrType: typeof(Point),
                oldType: "geometry",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GasStations_Location",
                table: "GasStations",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "GIST");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GasStations_Location",
                table: "GasStations");

            migrationBuilder.AlterColumn<Point>(
                name: "Location",
                table: "GasStations",
                type: "geometry",
                nullable: true,
                oldClrType: typeof(Point),
                oldType: "geography",
                oldNullable: true,
                oldComputedColumnSql: "ST_SetSRID(ST_MakePoint(\"Longitude\", \"Latitude\"), 4326)");
        }
    }
}
