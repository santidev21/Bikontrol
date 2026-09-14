using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bikontrol.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDataIntegrityGuards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "users",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "UserMaintenanceTypes",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Motorcycles",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserMaintenanceTypes_PositiveInterval",
                table: "UserMaintenanceTypes",
                sql: "(\"TrackingType\" = 'Km' AND \"KmInterval\" IS NOT NULL AND \"KmInterval\" > 0) OR (\"TrackingType\" = 'Time' AND \"TimeIntervalWeeks\" IS NOT NULL AND \"TimeIntervalWeeks\" > 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MotorcycleMaintenanceRecords_PerformedKm_NonNegative",
                table: "MotorcycleMaintenanceRecords",
                sql: "\"PerformedKm\" IS NULL OR \"PerformedKm\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MotorcycleKmHistories_Km_NonNegative",
                table: "MotorcycleKmHistories",
                sql: "\"Km\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_UserMaintenanceTypes_PositiveInterval",
                table: "UserMaintenanceTypes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MotorcycleMaintenanceRecords_PerformedKm_NonNegative",
                table: "MotorcycleMaintenanceRecords");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MotorcycleKmHistories_Km_NonNegative",
                table: "MotorcycleKmHistories");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "users");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "UserMaintenanceTypes");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Motorcycles");
        }
    }
}
