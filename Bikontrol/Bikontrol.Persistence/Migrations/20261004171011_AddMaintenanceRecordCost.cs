using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bikontrol.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceRecordCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Cost",
                table: "MotorcycleMaintenanceRecords",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_MotorcycleMaintenanceRecords_Cost_NonNegative",
                table: "MotorcycleMaintenanceRecords",
                sql: "\"Cost\" IS NULL OR \"Cost\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_MotorcycleMaintenanceRecords_Cost_NonNegative",
                table: "MotorcycleMaintenanceRecords");

            migrationBuilder.DropColumn(
                name: "Cost",
                table: "MotorcycleMaintenanceRecords");
        }
    }
}
