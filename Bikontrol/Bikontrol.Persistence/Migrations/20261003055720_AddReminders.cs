using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bikontrol.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RemindersEnabled",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "reminder_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserMaintenanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    MotorcycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsOverdue = table.Column<bool>(type: "boolean", nullable: false),
                    LifePercent = table.Column<int>(type: "integer", nullable: false),
                    RemainingKm = table.Column<int>(type: "integer", nullable: false),
                    RemainingDays = table.Column<int>(type: "integer", nullable: false),
                    Channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reminder_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reminder_logs_UserMaintenanceTypes_UserMaintenanceId",
                        column: x => x.UserMaintenanceId,
                        principalTable: "UserMaintenanceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_reminder_logs_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_reminder_logs_UserId",
                table: "reminder_logs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_reminder_logs_UserMaintenanceId_Kind_CreatedAt",
                table: "reminder_logs",
                columns: new[] { "UserMaintenanceId", "Kind", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reminder_logs");

            migrationBuilder.DropColumn(
                name: "RemindersEnabled",
                table: "users");
        }
    }
}
