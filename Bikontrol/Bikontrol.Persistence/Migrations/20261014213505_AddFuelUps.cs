using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bikontrol.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFuelUps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AnnualMaintenanceBudget",
                table: "users",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyMaintenanceBudget",
                table: "users",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FuelUps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MotorcycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    FueledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OdometerKm = table.Column<int>(type: "integer", nullable: false),
                    Litres = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    IsFullTank = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Station = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelUps", x => x.Id);
                    table.CheckConstraint("CK_FuelUps_Litres_Positive", "\"Litres\" > 0");
                    table.CheckConstraint("CK_FuelUps_OdometerKm_NonNegative", "\"OdometerKm\" >= 0");
                    table.CheckConstraint("CK_FuelUps_TotalCost_NonNegative", "\"TotalCost\" >= 0");
                    table.ForeignKey(
                        name: "FK_FuelUps_Motorcycles_MotorcycleId",
                        column: x => x.MotorcycleId,
                        principalTable: "Motorcycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FuelUps_MotorcycleId_FueledAt",
                table: "FuelUps",
                columns: new[] { "MotorcycleId", "FueledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FuelUps_MotorcycleId_OdometerKm",
                table: "FuelUps",
                columns: new[] { "MotorcycleId", "OdometerKm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FuelUps");

            migrationBuilder.DropColumn(
                name: "AnnualMaintenanceBudget",
                table: "users");

            migrationBuilder.DropColumn(
                name: "MonthlyMaintenanceBudget",
                table: "users");
        }
    }
}
