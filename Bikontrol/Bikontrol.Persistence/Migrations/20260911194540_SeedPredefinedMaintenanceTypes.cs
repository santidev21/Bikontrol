using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bikontrol.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedPredefinedMaintenanceTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Re-seeds the 26 predefined maintenance types. The 20260522035556_CleanupAllButUsers
            // migration truncates MaintenanceTypes (its 'Users' exclusion is case-sensitive and does
            // not cover this table), wiping the InitialCreate seed on fresh and existing databases.
            // ON CONFLICT DO NOTHING makes this idempotent: safe on empty and already-seeded DBs.
            migrationBuilder.Sql(@"
INSERT INTO ""MaintenanceTypes"" (""Id"", ""Name"", ""Description"", ""DefaultKmInterval"", ""DefaultTimeIntervalWeeks"", ""TrackingType"", ""IsEnabled"") VALUES
  ('10000000-0000-0000-0000-000000000001', 'Cambio de Aceite', 'Reemplazo del aceite del motor.', 1500, 6, 'Km', TRUE),
  ('10000000-0000-0000-0000-000000000002', 'Cambio de Filtro de Aceite', 'Sustitución del filtro de aceite del motor.', 1500, 6, 'Km', TRUE),
  ('10000000-0000-0000-0000-000000000003', 'Lubricación y Limpieza de Cadena', 'Limpieza y lubricación de la cadena de transmisión.', 500, 2, 'Time', TRUE),
  ('10000000-0000-0000-0000-000000000004', 'Revisión General', 'Chequeo completo del estado general de la motocicleta.', 20000, 80, 'Km', TRUE),
  ('10000000-0000-0000-0000-000000000005', 'Mantenimiento Preventivo', 'Revisión preventiva del estado general de la motocicleta.', 5000, 20, 'Km', TRUE),
  ('10000000-0000-0000-0000-000000000006', 'Filtro de Aire', 'Limpieza o reemplazo del filtro de aire.', 6000, 24, 'Km', TRUE),
  ('10000000-0000-0000-0000-000000000007', 'Filtro de Gasolina', 'Reemplazo o limpieza del filtro de gasolina.', 8000, 32, 'Km', TRUE),
  ('10000000-0000-0000-0000-000000000008', 'Presión de Llantas', 'Verificación y ajuste de la presión de las llantas.', 500, 1, 'Time', TRUE),
  ('10000000-0000-0000-0000-000000000009', 'Pastillas de Freno Delanteras', 'Revisión y reemplazo de las pastillas de freno delanteras.', 8000, 32, 'Km', TRUE),
  ('10000000-0000-0000-0000-000000000010', 'Pastillas de Freno Traseras', 'Revisión y reemplazo de las pastillas de freno traseras.', 8000, 32, 'Km', TRUE),
  ('10000000-0000-0000-0000-000000000011', 'Disco de Freno Delantero', 'Revisión del disco de freno delantero.', 25000, 100, 'Km', TRUE),
  ('10000000-0000-0000-0000-000000000012', 'Disco de Freno Trasero', 'Revisión del disco de freno trasero.', 25000, 100, 'Km', TRUE),
  ('10000000-0000-0000-0000-000000000013', 'Líquido de Frenos Delantero', 'Reemplazo del líquido de frenos delantero.', 13000, 52, 'Time', TRUE),
  ('10000000-0000-0000-0000-000000000014', 'Líquido de Frenos Trasero', 'Reemplazo del líquido de frenos trasero.', 13000, 52, 'Time', TRUE),
  ('10000000-0000-0000-0000-000000000015', 'Batería', 'Revisión del estado y carga de la batería.', 10000, 52, 'Time', TRUE),
  ('10000000-0000-0000-0000-000000000016', 'Suspensión', 'Revisión de suspensión delantera y trasera.', 10000, 40, 'Km', TRUE),
  ('10000000-0000-0000-0000-000000000017', 'Tornillería', 'Ajuste general de tornillería.', 2000, 8, 'Time', TRUE),
  ('10000000-0000-0000-0000-000000000018', 'Calibración de Válvulas', 'Ajuste de holguras de válvulas.', 8000, 32, 'Km', TRUE),
  ('10000000-0000-0000-0000-000000000019', 'Kit de Arrastre - Piñón', 'Revisión o reemplazo del piñón.', 12000, 48, 'Km', TRUE),
  ('10000000-0000-0000-0000-000000000020', 'Kit de Arrastre - Corona', 'Revisión o reemplazo de la corona.', 12000, 48, 'Km', TRUE),
  ('10000000-0000-0000-0000-000000000021', 'Kit de Arrastre - Cadena', 'Revisión o reemplazo de la cadena.', 12000, 48, 'Km', TRUE),
  ('10000000-0000-0000-0000-000000000022', 'Tensión de Cadena', 'Ajuste de la tensión de la cadena.', 500, 2, 'Km', TRUE),
  ('10000000-0000-0000-0000-000000000023', 'Sincronización', 'Ajuste de mezcla/ralentí o sistema de inyección.', 8000, 32, 'Km', TRUE),
  ('10000000-0000-0000-0000-000000000024', 'Bandas de Freno', 'Reemplazo de bandas de freno (frenos de tambor).', 8000, 32, 'Km', TRUE),
  ('10000000-0000-0000-0000-000000000025', 'Bujía', 'Revisión o reemplazo de bujía.', 4000, 16, 'Km', TRUE),
  ('10000000-0000-0000-0000-000000000026', 'Neumáticos', 'Revisión del desgaste de los neumáticos.', 15000, 60, 'Km', TRUE)
ON CONFLICT (""Id"") DO NOTHING;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Safe: UserMaintenanceTypes.BaseTypeId FK is ON DELETE SET NULL.
            migrationBuilder.Sql(@"
DELETE FROM ""MaintenanceTypes""
WHERE ""Id"" BETWEEN '10000000-0000-0000-0000-000000000001' AND '10000000-0000-0000-0000-000000000026';");
        }
    }
}
