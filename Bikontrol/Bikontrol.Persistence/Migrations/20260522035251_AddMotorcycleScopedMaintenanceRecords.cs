using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bikontrol.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMotorcycleScopedMaintenanceRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM ""UserMaintenanceTypes"";");

            migrationBuilder.DropIndex(
                name: "IX_UserMaintenanceTypes_UserId",
                table: "UserMaintenanceTypes");

            migrationBuilder.AddColumn<Guid>(
                name: "MotorcycleId",
                table: "UserMaintenanceTypes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "MotorcycleMaintenanceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MotorcycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserMaintenanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PerformedKm = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MotorcycleMaintenanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MotorcycleMaintenanceRecords_Motorcycles_MotorcycleId",
                        column: x => x.MotorcycleId,
                        principalTable: "Motorcycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MotorcycleMaintenanceRecords_UserMaintenanceTypes_UserMaint~",
                        column: x => x.UserMaintenanceId,
                        principalTable: "UserMaintenanceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserMaintenanceTypes_MotorcycleId",
                table: "UserMaintenanceTypes",
                column: "MotorcycleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMaintenanceTypes_UserId_MotorcycleId_Name",
                table: "UserMaintenanceTypes",
                columns: new[] { "UserId", "MotorcycleId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_MotorcycleMaintenanceRecords_MotorcycleId_PerformedAt",
                table: "MotorcycleMaintenanceRecords",
                columns: new[] { "MotorcycleId", "PerformedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MotorcycleMaintenanceRecords_UserMaintenanceId_PerformedAt",
                table: "MotorcycleMaintenanceRecords",
                columns: new[] { "UserMaintenanceId", "PerformedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_UserMaintenanceTypes_Motorcycles_MotorcycleId",
                table: "UserMaintenanceTypes",
                column: "MotorcycleId",
                principalTable: "Motorcycles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserMaintenanceTypes_Motorcycles_MotorcycleId",
                table: "UserMaintenanceTypes");

            migrationBuilder.DropTable(
                name: "MotorcycleMaintenanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_UserMaintenanceTypes_MotorcycleId",
                table: "UserMaintenanceTypes");

            migrationBuilder.DropIndex(
                name: "IX_UserMaintenanceTypes_UserId_MotorcycleId_Name",
                table: "UserMaintenanceTypes");

            migrationBuilder.DropColumn(
                name: "MotorcycleId",
                table: "UserMaintenanceTypes");

            migrationBuilder.CreateIndex(
                name: "IX_UserMaintenanceTypes_UserId",
                table: "UserMaintenanceTypes",
                column: "UserId");
        }
    }
}
