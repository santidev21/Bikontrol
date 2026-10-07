-- ---------------------------------------------------------------------------
-- Bikontrol — pre-flight queries for seeding a user's motorcycle.
--
-- Run these (read-only) against the environment you want to seed and copy the
-- two ids into scripts/seed-maintenances.mjs. Replace the email if needed.
--
-- Production (from the deploy host, against the DB container):
--   docker compose exec -T db psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" \
--     -f - < scripts/db-inspect-user.sql
--
-- Local (Docker dev DB on 127.0.0.1:5434):
--   docker exec -i bikontrol-db psql -U bikontrol -d bikontrol_db \
--     -f - < scripts/db-inspect-user.sql
--
-- Edit the email in the \set line below if you need another account.
-- ---------------------------------------------------------------------------

\set owner_email 'orsantiago21@gmail.com'

-- 1) User id (+ basic sanity).
SELECT "Id" AS user_id, "Email", "FullName", "Role", "CreatedAt"
FROM users
WHERE "Email" = :'owner_email';

-- 2) The user's active motorcycles. Copy the wanted `motorcycle_id`.
SELECT m."Id" AS motorcycle_id, m."Name", m."Brand", m."Year",
       m."Nickname", m."Plate", m."Displacement", m."IsEnabled"
FROM "Motorcycles" m
JOIN users u ON u."Id" = m."UserId"
WHERE u."Email" = :'owner_email'
  AND m."IsEnabled" = true
ORDER BY m."Year" DESC;

-- 3) Odometer readings of that motorcycle (latest row = current km used to
--    distribute the maintenance history). Assumes a single active motorcycle;
--    if there are several, replace the subquery with the `motorcycle_id`.
SELECT "Km", "RecordedAt"
FROM "MotorcycleKmHistories"
WHERE "MotorcycleId" = (
    SELECT m."Id"
    FROM "Motorcycles" m
    JOIN users u ON u."Id" = m."UserId"
    WHERE u."Email" = :'owner_email' AND m."IsEnabled" = true
    ORDER BY m."Year" DESC
    LIMIT 1
)
ORDER BY "RecordedAt" DESC;

-- 4) What is already seeded for that motorcycle (avoid duplicates).
SELECT um."Id" AS user_maintenance_id, um."Name", um."TrackingType",
       um."KmInterval", um."TimeIntervalWeeks", um."IsEnabled",
       count(r."Id") AS records
FROM "UserMaintenanceTypes" um
LEFT JOIN "MotorcycleMaintenanceRecords" r ON r."UserMaintenanceId" = um."Id"
WHERE um."MotorcycleId" = (
    SELECT m."Id"
    FROM "Motorcycles" m
    JOIN users u ON u."Id" = m."UserId"
    WHERE u."Email" = :'owner_email' AND m."IsEnabled" = true
    ORDER BY m."Year" DESC
    LIMIT 1
)
GROUP BY um."Id", um."Name", um."TrackingType", um."KmInterval",
         um."TimeIntervalWeeks", um."IsEnabled"
ORDER BY um."Name";
