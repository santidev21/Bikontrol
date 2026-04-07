using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Bikontrol.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MaintenanceTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DefaultKmInterval = table.Column<int>(type: "integer", nullable: true),
                    DefaultTimeIntervalWeeks = table.Column<int>(type: "integer", nullable: true),
                    TrackingType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "Km"),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Motorcycles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Nickname = table.Column<string>(type: "text", nullable: false),
                    Image = table.Column<string>(type: "text", nullable: false, defaultValue: "default.png"),
                    Displacement = table.Column<int>(type: "integer", nullable: false),
                    Plate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Motorcycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Motorcycles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserMaintenanceTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BaseTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    KmInterval = table.Column<int>(type: "integer", nullable: true),
                    TimeIntervalWeeks = table.Column<int>(type: "integer", nullable: true),
                    TrackingType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "Km"),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMaintenanceTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserMaintenanceTypes_MaintenanceTypes_BaseTypeId",
                        column: x => x.BaseTypeId,
                        principalTable: "MaintenanceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserMaintenanceTypes_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MotorcycleKmHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MotorcycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Km = table.Column<int>(type: "integer", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MotorcycleKmHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MotorcycleKmHistories_Motorcycles_MotorcycleId",
                        column: x => x.MotorcycleId,
                        principalTable: "Motorcycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "MaintenanceTypes",
                columns: new[] { "Id", "DefaultKmInterval", "DefaultTimeIntervalWeeks", "Description", "IsEnabled", "Name", "TrackingType" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), 1500, 6, "Reemplazo del aceite del motor.", true, "Cambio de Aceite", "Km" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), 1500, 6, "Sustitución del filtro de aceite del motor.", true, "Cambio de Filtro de Aceite", "Km" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), 500, 2, "Limpieza y lubricación de la cadena de transmisión.", true, "Lubricación y Limpieza de Cadena", "Time" },
                    { new Guid("10000000-0000-0000-0000-000000000004"), 20000, 80, "Chequeo completo del estado general de la motocicleta.", true, "Revisión General", "Km" },
                    { new Guid("10000000-0000-0000-0000-000000000005"), 5000, 20, "Revisión preventiva del estado general de la motocicleta.", true, "Mantenimiento Preventivo", "Km" },
                    { new Guid("10000000-0000-0000-0000-000000000006"), 6000, 24, "Limpieza o reemplazo del filtro de aire.", true, "Filtro de Aire", "Km" },
                    { new Guid("10000000-0000-0000-0000-000000000007"), 8000, 32, "Reemplazo o limpieza del filtro de gasolina.", true, "Filtro de Gasolina", "Km" },
                    { new Guid("10000000-0000-0000-0000-000000000008"), 500, 1, "Verificación y ajuste de la presión de las llantas.", true, "Presión de Llantas", "Time" },
                    { new Guid("10000000-0000-0000-0000-000000000009"), 8000, 32, "Revisión y reemplazo de las pastillas de freno delanteras.", true, "Pastillas de Freno Delanteras", "Km" },
                    { new Guid("10000000-0000-0000-0000-000000000010"), 8000, 32, "Revisión y reemplazo de las pastillas de freno traseras.", true, "Pastillas de Freno Traseras", "Km" },
                    { new Guid("10000000-0000-0000-0000-000000000011"), 25000, 100, "Revisión del disco de freno delantero.", true, "Disco de Freno Delantero", "Km" },
                    { new Guid("10000000-0000-0000-0000-000000000012"), 25000, 100, "Revisión del disco de freno trasero.", true, "Disco de Freno Trasero", "Km" },
                    { new Guid("10000000-0000-0000-0000-000000000013"), 13000, 52, "Reemplazo del líquido de frenos delantero.", true, "Líquido de Frenos Delantero", "Time" },
                    { new Guid("10000000-0000-0000-0000-000000000014"), 13000, 52, "Reemplazo del líquido de frenos trasero.", true, "Líquido de Frenos Trasero", "Time" },
                    { new Guid("10000000-0000-0000-0000-000000000015"), 10000, 52, "Revisión del estado y carga de la batería.", true, "Batería", "Time" },
                    { new Guid("10000000-0000-0000-0000-000000000016"), 10000, 40, "Revisión de suspensión delantera y trasera.", true, "Suspensión", "Km" },
                    { new Guid("10000000-0000-0000-0000-000000000017"), 2000, 8, "Ajuste general de tornillería.", true, "Tornillería", "Time" },
                    { new Guid("10000000-0000-0000-0000-000000000018"), 8000, 32, "Ajuste de holguras de válvulas.", true, "Calibración de Válvulas", "Km" },
                    { new Guid("10000000-0000-0000-0000-000000000019"), 12000, 48, "Revisión o reemplazo del piñón.", true, "Kit de Arrastre - Piñón", "Km" },
                    { new Guid("10000000-0000-0000-0000-000000000020"), 12000, 48, "Revisión o reemplazo de la corona.", true, "Kit de Arrastre - Corona", "Km" },
                    { new Guid("10000000-0000-0000-0000-000000000021"), 12000, 48, "Revisión o reemplazo de la cadena.", true, "Kit de Arrastre - Cadena", "Km" },
                    { new Guid("10000000-0000-0000-0000-000000000022"), 500, 2, "Ajuste de la tensión de la cadena.", true, "Tensión de Cadena", "Km" },
                    { new Guid("10000000-0000-0000-0000-000000000023"), 8000, 32, "Ajuste de mezcla/ralentí o sistema de inyección.", true, "Sincronización", "Km" },
                    { new Guid("10000000-0000-0000-0000-000000000024"), 8000, 32, "Reemplazo de bandas de freno (frenos de tambor).", true, "Bandas de Freno", "Km" },
                    { new Guid("10000000-0000-0000-0000-000000000025"), 4000, 16, "Revisión o reemplazo de bujía.", true, "Bujía", "Km" },
                    { new Guid("10000000-0000-0000-0000-000000000026"), 15000, 60, "Revisión del desgaste de los neumáticos.", true, "Neumáticos", "Km" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_MotorcycleKmHistories_MotorcycleId_RecordedAt",
                table: "MotorcycleKmHistories",
                columns: new[] { "MotorcycleId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Motorcycles_UserId",
                table: "Motorcycles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMaintenanceTypes_BaseTypeId",
                table: "UserMaintenanceTypes",
                column: "BaseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMaintenanceTypes_UserId",
                table: "UserMaintenanceTypes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MotorcycleKmHistories");

            migrationBuilder.DropTable(
                name: "UserMaintenanceTypes");

            migrationBuilder.DropTable(
                name: "Motorcycles");

            migrationBuilder.DropTable(
                name: "MaintenanceTypes");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
