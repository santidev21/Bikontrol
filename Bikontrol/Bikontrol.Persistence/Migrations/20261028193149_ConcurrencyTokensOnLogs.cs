using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bikontrol.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConcurrencyTokensOnLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "MotorcycleMaintenanceRecords",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "MotorcycleKmHistories",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "FuelUps",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "MotorcycleMaintenanceRecords");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "MotorcycleKmHistories");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "FuelUps");
        }
    }
}
