-- ============================================================================
-- Bikontrol · Auditoría de integridad de datos (SOLO LECTURA)
-- Uso: psql "$DATABASE_URL" -f scripts/db-integrity-audit.sql
-- Cada bloque debe devolver 0 filas. Si alguno devuelve filas, corregir los
-- datos ANTES de aplicar migraciones o desplegar cambios de esquema.
-- ============================================================================

\pset footer off

-- 1. Motocicletas sin historial de kilometraje (el resumen las vería en 0 km)
SELECT '1. Motos sin km-history' AS check_name, m."Id", m."UserId", m."Nickname"
FROM "Motorcycles" m
LEFT JOIN "MotorcycleKmHistories" h ON h."MotorcycleId" = m."Id"
WHERE m."IsEnabled" AND h."Id" IS NULL;

-- 2. Mantenimientos activos con intervalo inválido según su TrackingType
-- (debe coincidir con CK_UserMaintenanceTypes_PositiveInterval)
SELECT '2. Intervalo inválido' AS check_name, u."Id", u."MotorcycleId", u."Name",
       u."TrackingType", u."KmInterval", u."TimeIntervalWeeks"
FROM "UserMaintenanceTypes" u
WHERE u."IsEnabled"
  AND NOT (
    (u."TrackingType" = 'Km' AND u."KmInterval" IS NOT NULL AND u."KmInterval" > 0)
    OR (u."TrackingType" = 'Time' AND u."TimeIntervalWeeks" IS NOT NULL AND u."TimeIntervalWeeks" > 0)
  );

-- 3. Kilometrajes negativos en historiales
SELECT '3. Km negativo' AS check_name, "Id", "MotorcycleId", "Km", "RecordedAt"
FROM "MotorcycleKmHistories"
WHERE "Km" < 0;

-- 4. PerformedKm negativos en registros
SELECT '4. PerformedKm negativo' AS check_name, "Id", "MotorcycleId",
       "UserMaintenanceId", "PerformedKm", "PerformedAt"
FROM "MotorcycleMaintenanceRecords"
WHERE "PerformedKm" IS NOT NULL AND "PerformedKm" < 0;

-- 5. Registros no monotónicos: un registro posterior (por fecha) con menor km
-- que el anterior del mismo mantenimiento
SELECT '5. Registro no monotónico' AS check_name, "UserMaintenanceId", "Id",
       "PerformedAt", "PerformedKm", prev_km
FROM (
    SELECT r.*,
           LAG(r."PerformedKm") OVER (
               PARTITION BY r."UserMaintenanceId"
               ORDER BY r."PerformedAt", r."CreatedAt") AS prev_km
    FROM "MotorcycleMaintenanceRecords" r
) s
WHERE "PerformedKm" IS NOT NULL AND prev_km IS NOT NULL AND "PerformedKm" < prev_km;

-- 6. Registros con PerformedKm mayor al odómetro actual de la moto
SELECT '6. PerformedKm > odómetro' AS check_name, r."Id", r."MotorcycleId",
       r."PerformedKm", odo."Km" AS current_km
FROM "MotorcycleMaintenanceRecords" r
JOIN LATERAL (
    SELECT h."Km"
    FROM "MotorcycleKmHistories" h
    WHERE h."MotorcycleId" = r."MotorcycleId"
    ORDER BY h."RecordedAt" DESC
    LIMIT 1
) odo ON true
WHERE r."PerformedKm" IS NOT NULL AND r."PerformedKm" > odo."Km";

-- 7. Registros cuya moto difiere de la moto del mantenimiento
SELECT '7. Registro en moto ajena' AS check_name, r."Id",
       r."MotorcycleId" AS record_moto, u."MotorcycleId" AS maintenance_moto
FROM "MotorcycleMaintenanceRecords" r
JOIN "UserMaintenanceTypes" u ON u."Id" = r."UserMaintenanceId"
WHERE r."MotorcycleId" <> u."MotorcycleId";

-- 8. Registros que apuntan a mantenimientos o motos deshabilitados
SELECT '8. Registro sobre soft-delete' AS check_name, r."Id",
       r."MotorcycleId", r."UserMaintenanceId"
FROM "MotorcycleMaintenanceRecords" r
LEFT JOIN "UserMaintenanceTypes" u ON u."Id" = r."UserMaintenanceId"
LEFT JOIN "Motorcycles" m ON m."Id" = r."MotorcycleId"
WHERE u."Id" IS NULL OR NOT u."IsEnabled" OR m."Id" IS NULL OR NOT m."IsEnabled";

-- 9. Historial de km no monótono por moto (bajada de odómetro entre registros)
SELECT '9. Odómetro no monótono' AS check_name, "MotorcycleId", "Id",
       "RecordedAt", "Km", prev_km
FROM (
    SELECT h.*,
           LAG(h."Km") OVER (
               PARTITION BY h."MotorcycleId"
               ORDER BY h."RecordedAt") AS prev_km
    FROM "MotorcycleKmHistories" h
) s
WHERE prev_km IS NOT NULL AND "Km" < prev_km;

-- 10. Registros con fecha futura
SELECT '10. PerformedAt futura' AS check_name, "Id", "MotorcycleId",
       "UserMaintenanceId", "PerformedAt"
FROM "MotorcycleMaintenanceRecords"
WHERE "PerformedAt" > now();

-- 11. Emails duplicados
SELECT '11. Email duplicado' AS check_name, "Email", count(*) AS total
FROM users
GROUP BY "Email"
HAVING count(*) > 1;

-- 12. Mantenimientos de motos de otro usuario (propiedad cruzada)
SELECT '12. Propiedad cruzada' AS check_name, u."Id", u."UserId" AS maint_user,
       m."UserId" AS moto_user
FROM "UserMaintenanceTypes" u
JOIN "Motorcycles" m ON m."Id" = u."MotorcycleId"
WHERE u."UserId" <> m."UserId";

-- 13-16. Fuel-ups y presupuestos. Estas tablas/columnas llegan con la migración
-- AddFuelUps, y esta auditoría se corre ANTES de aplicarla: se saltan mientras no
-- existan (si no, el script fallaría con "relation does not exist").
SELECT to_regclass('"FuelUps"') IS NOT NULL AS fuelups_exists \gset
\if :fuelups_exists
-- 13. Cargas de combustible con valores fuera de rango (debe coincidir con las
-- CHECK constraints CK_FuelUps_*)
SELECT '13. Carga inválida' AS check_name, "Id", "MotorcycleId",
       "OdometerKm", "Litres", "TotalCost"
FROM "FuelUps"
WHERE "OdometerKm" < 0 OR "Litres" <= 0 OR "TotalCost" < 0;

-- 14. Cargas con fecha futura
SELECT '14. FueledAt futura' AS check_name, "Id", "MotorcycleId", "FueledAt"
FROM "FuelUps"
WHERE "FueledAt" > now();

-- 15. Cargas que apuntan a motos deshabilitadas (soft-delete)
SELECT '15. Carga sobre soft-delete' AS check_name, f."Id", f."MotorcycleId"
FROM "FuelUps" f
LEFT JOIN "Motorcycles" m ON m."Id" = f."MotorcycleId"
WHERE m."Id" IS NULL OR NOT m."IsEnabled";
\endif

SELECT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_name = 'users' AND column_name = 'MonthlyMaintenanceBudget'
) AS budget_columns_exist \gset
\if :budget_columns_exist
-- 16. Presupuestos de mantenimiento negativos
SELECT '16. Presupuesto negativo' AS check_name, "Id", "Email",
       "MonthlyMaintenanceBudget", "AnnualMaintenanceBudget"
FROM users
WHERE "MonthlyMaintenanceBudget" < 0 OR "AnnualMaintenanceBudget" < 0;
\endif
