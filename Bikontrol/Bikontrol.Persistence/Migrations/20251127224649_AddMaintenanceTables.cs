using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Bikontrol.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceTables : Migration
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
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceTypes", x => x.Id);
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

            migrationBuilder.InsertData(
                table: "MaintenanceTypes",
                columns: new[] { "Id", "DefaultKmInterval", "DefaultTimeIntervalWeeks", "Description", "IsEnabled", "Name" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), 1500, null, "Reemplazo del aceite del motor.", true, "Cambio de Aceite" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), 1500, null, "Sustitución del filtro de aceite del motor.", true, "Cambio de Filtro de Aceite" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), 500, 2, "Limpieza y lubricación de la cadena de transmisión.", true, "Lubricación y Limpieza de Cadena" },
                    { new Guid("10000000-0000-0000-0000-000000000004"), 20000, null, "Chequeo completo del estado general de la motocicleta.", true, "Revisión General" },
                    { new Guid("10000000-0000-0000-0000-000000000005"), 5000, null, "Revisión preventiva del estado general de la motocicleta.", true, "Mantenimiento Preventivo" },
                    { new Guid("10000000-0000-0000-0000-000000000006"), 6000, null, "Limpieza o reemplazo del filtro de aire.", true, "Filtro de Aire" },
                    { new Guid("10000000-0000-0000-0000-000000000007"), 8000, null, "Reemplazo o limpieza del filtro de gasolina.", true, "Filtro de Gasolina" },
                    { new Guid("10000000-0000-0000-0000-000000000008"), null, 1, "Verificación y ajuste de la presión de las llantas.", true, "Presión de Llantas" },
                    { new Guid("10000000-0000-0000-0000-000000000009"), 8000, null, "Revisión y reemplazo de las pastillas de freno delanteras.", true, "Pastillas de Freno Delanteras" },
                    { new Guid("10000000-0000-0000-0000-000000000010"), 8000, null, "Revisión y reemplazo de las pastillas de freno traseras.", true, "Pastillas de Freno Traseras" },
                    { new Guid("10000000-0000-0000-0000-000000000011"), 25000, null, "Revisión del disco de freno delantero.", true, "Disco de Freno Delantero" },
                    { new Guid("10000000-0000-0000-0000-000000000012"), 25000, null, "Revisión del disco de freno trasero.", true, "Disco de Freno Trasero" },
                    { new Guid("10000000-0000-0000-0000-000000000013"), null, 52, "Reemplazo del líquido de frenos delantero.", true, "Líquido de Frenos Delantero" },
                    { new Guid("10000000-0000-0000-0000-000000000014"), null, 52, "Reemplazo del líquido de frenos trasero.", true, "Líquido de Frenos Trasero" },
                    { new Guid("10000000-0000-0000-0000-000000000015"), null, 52, "Revisión del estado y carga de la batería.", true, "Batería" },
                    { new Guid("10000000-0000-0000-0000-000000000016"), 10000, null, "Revisión de suspensión delantera y trasera.", true, "Suspensión" },
                    { new Guid("10000000-0000-0000-0000-000000000017"), null, 8, "Ajuste general de tornillería.", true, "Tornillería" },
                    { new Guid("10000000-0000-0000-0000-000000000018"), 8000, null, "Ajuste de holguras de válvulas.", true, "Calibración de Válvulas" },
                    { new Guid("10000000-0000-0000-0000-000000000019"), 12000, null, "Revisión o reemplazo del piñón.", true, "Kit de Arrastre - Piñón" },
                    { new Guid("10000000-0000-0000-0000-000000000020"), 12000, null, "Revisión o reemplazo de la corona.", true, "Kit de Arrastre - Corona" },
                    { new Guid("10000000-0000-0000-0000-000000000021"), 12000, null, "Revisión o reemplazo de la cadena.", true, "Kit de Arrastre - Cadena" },
                    { new Guid("10000000-0000-0000-0000-000000000022"), 500, 2, "Ajuste de la tensión de la cadena.", true, "Tensión de Cadena" },
                    { new Guid("10000000-0000-0000-0000-000000000023"), 8000, null, "Ajuste de mezcla/ralentí o sistema de inyección.", true, "Sincronización" },
                    { new Guid("10000000-0000-0000-0000-000000000024"), 8000, null, "Reemplazo de bandas de freno (frenos de tambor).", true, "Bandas de Freno" },
                    { new Guid("10000000-0000-0000-0000-000000000025"), 4000, null, "Revisión o reemplazo de bujía.", true, "Bujía" },
                    { new Guid("10000000-0000-0000-0000-000000000026"), 15000, null, "Revisión del desgaste de los neumáticos.", true, "Neumáticos" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserMaintenanceTypes_BaseTypeId",
                table: "UserMaintenanceTypes",
                column: "BaseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMaintenanceTypes_UserId",
                table: "UserMaintenanceTypes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserMaintenanceTypes");

            migrationBuilder.DropTable(
                name: "MaintenanceTypes");
        }
    }
}
