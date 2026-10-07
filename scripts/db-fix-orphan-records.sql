-- ============================================================================
-- Bikontrol · Remediación de registros de mantenimiento huérfanos
--
-- Corrige los hallazgos 5 y 8 de scripts/db-integrity-audit.sql: registros de
-- mantenimiento que apuntan a una moto o a un mantenimiento deshabilitado
-- (soft-delete) o inexistente. Como la app filtra IsEnabled en todos lados,
-- esos registros ya no son alcanzables, así que se eliminan. Los adjuntos se
-- borran en cascada (FK maintenance_record_attachments -> MotorcycleMaintenanceRecords).
--
-- Uso (host de deploy):
--   docker compose exec -T db psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" \
--     -f - < scripts/db-fix-orphan-records.sql
--
-- OJO: si el mantenimiento deshabilitado es un "unfollow" reversible
-- (FollowDefaultAsync lo vuelve a habilitar), borrar sus registros pierde ese
-- historial. Revisá el bloque PREVIEW antes de aplicar: si esos registros te
-- importan, la alternativa es re-habilitar el mantenimiento en vez de borrar.
-- ============================================================================

\pset footer off

-- ---------------------------------------------------------------------------
-- PREVIEW — qué se borraría (no modifica nada). Revisá que tenga sentido.
-- ---------------------------------------------------------------------------
SELECT 'orphan record' AS kind, r."Id", r."MotorcycleId", r."UserMaintenanceId",
       r."PerformedAt", r."PerformedKm"
FROM "MotorcycleMaintenanceRecords" r
LEFT JOIN "UserMaintenanceTypes" u ON u."Id" = r."UserMaintenanceId"
LEFT JOIN "Motorcycles" m ON m."Id" = r."MotorcycleId"
WHERE u."Id" IS NULL OR NOT u."IsEnabled" OR m."Id" IS NULL OR NOT m."IsEnabled"
ORDER BY r."PerformedAt";

-- ---------------------------------------------------------------------------
-- APPLY — borra los registros huérfanos (transacción)
-- ---------------------------------------------------------------------------
BEGIN;

WITH orphans AS (
    SELECT r."Id"
    FROM "MotorcycleMaintenanceRecords" r
    LEFT JOIN "UserMaintenanceTypes" u ON u."Id" = r."UserMaintenanceId"
    LEFT JOIN "Motorcycles" m ON m."Id" = r."MotorcycleId"
    WHERE u."Id" IS NULL OR NOT u."IsEnabled" OR m."Id" IS NULL OR NOT m."IsEnabled"
)
DELETE FROM "MotorcycleMaintenanceRecords"
WHERE "Id" IN (SELECT "Id" FROM orphans);

COMMIT;

-- ---------------------------------------------------------------------------
-- VERIFY — debe devolver 0 filas.
-- ---------------------------------------------------------------------------
SELECT '8. Registro sobre soft-delete' AS check_name, r."Id",
       r."MotorcycleId", r."UserMaintenanceId"
FROM "MotorcycleMaintenanceRecords" r
LEFT JOIN "UserMaintenanceTypes" u ON u."Id" = r."UserMaintenanceId"
LEFT JOIN "Motorcycles" m ON m."Id" = r."MotorcycleId"
WHERE u."Id" IS NULL OR NOT u."IsEnabled" OR m."Id" IS NULL OR NOT m."IsEnabled";
