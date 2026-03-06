using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TankWatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGeocodeTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GeocodeAttempts",
                table: "GasStations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastGeocodeAttempt",
                table: "GasStations",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeocodeAttempts",
                table: "GasStations");

            migrationBuilder.DropColumn(
                name: "LastGeocodeAttempt",
                table: "GasStations");
        }
    }
}
