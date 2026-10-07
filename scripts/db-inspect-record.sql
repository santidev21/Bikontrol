-- ---------------------------------------------------------------------------
-- Bikontrol — forensics for a maintenance record that "didn't appear".
--
-- Read-only. Run it against PRODUCTION and paste the output. It answers:
--   1. Which maintenance is the oil one and what it expects.
--   2. Every record logged for it (is the 24.000 km one there or not?).
--   3. The recent odometer history (who moved it to 24.000, and when?).
--   4. The audit trail: was a MotorcycleMaintenanceRecord ever Created/Deleted?
--
-- On the deploy host:
--   docker compose exec -T db psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" \
--     -f - < scripts/db-inspect-record.sql
-- ---------------------------------------------------------------------------

\set owner_email 'orsantiago21@gmail.com'
\set moto_id '1159ff4b-3fb7-411c-9b8e-c2cbdd0e0a40'

-- 1) The oil maintenance (and its id, to drill into records).
SELECT um."Id" AS user_maintenance_id, um."Name", um."TrackingType",
       um."KmInterval", um."TimeIntervalWeeks", um."IsEnabled"
FROM "UserMaintenanceTypes" um
WHERE um."MotorcycleId" = :'moto_id'::uuid
  AND um."Name" ILIKE '%aceite del motor%';

-- 2) All records for that maintenance, newest first.
SELECT r."Id", r."PerformedAt", r."PerformedKm", r."Cost", r."CreatedAt"
FROM "MotorcycleMaintenanceRecords" r
WHERE r."UserMaintenanceId" IN (
    SELECT um."Id" FROM "UserMaintenanceTypes" um
    WHERE um."MotorcycleId" = :'moto_id'::uuid
      AND um."Name" ILIKE '%aceite del motor%')
ORDER BY r."PerformedAt" DESC;

-- 3) Recent odometer history (latest readings first).
SELECT h."Id", h."Km", h."RecordedAt"
FROM "MotorcycleKmHistories" h
WHERE h."MotorcycleId" = :'moto_id'::uuid
ORDER BY h."RecordedAt" DESC
LIMIT 10;

-- 4) Audit trail: did the record get Created and/or Deleted?
SELECT a."Id", a."CreatedAt", a."Action", a."EntityName", a."EntityId", a."Changes"
FROM audit_logs a
JOIN users u ON u."Id" = a."UserId"
WHERE u."Email" = :'owner_email'
  AND a."EntityName" IN ('MotorcycleMaintenanceRecord', 'MotorcycleKmHistory')
ORDER BY a."Id" DESC
LIMIT 40;
