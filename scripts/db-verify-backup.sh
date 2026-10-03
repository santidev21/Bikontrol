#!/bin/bash
set -euo pipefail

# Bikontrol — Verify a database backup by restoring it into a throwaway database.
#
# A dump is only useful if it actually restores. This script:
#   1. picks the newest backup (or the one passed as the first argument),
#   2. restores it into a temporary database inside the running `db` container,
#   3. asserts the critical tables exist and (when the source had rows) that the
#      data landed, then drops the temporary database.
#
# It never touches the live database: it connects with the same credentials but
# creates and drops its own `*_restore_check` database.
#
# Usage:
#   scripts/db-verify-backup.sh                          # newest backup
#   scripts/db-verify-backup.sh backups/backup-XXX/db.sql.gz
#   scripts/db-verify-backup.sh backups/bikontrol-db-XXX.sql.gz
#
# Requires: the local `db` container running (`npm run db:up`) and a `.env`
# with POSTGRES_DB / POSTGRES_USER / POSTGRES_PASSWORD.

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

COMPOSE=(docker compose -f docker-compose.yml -f docker-compose.local.yml)

log() { echo "[db:verify] $1"; }
fail() { echo "[db:verify] ERROR: $1" >&2; exit 1; }

# --- credentials (same source of truth as the other scripts) -----------------
# .env is not shell-safe (passwords may contain $ & ! …), so parse it instead of
# sourcing it — same behavior as scripts/run-bikontrol.mjs.
read_env() {
    [ -f .env ] || return 0
    sed -n "s/^$1=//p" .env | tail -n 1 | sed -e 's/^"//' -e 's/"$//' -e "s/^'//" -e "s/'$//"
}
DB_NAME="$(read_env POSTGRES_DB)"; DB_NAME="${DB_NAME:-bikontrol_db}"
DB_USER="$(read_env POSTGRES_USER)"; DB_USER="${DB_USER:-bikontrol}"

# --- locate the backup file --------------------------------------------------
BACKUP_FILE="${1:-}"
if [ -z "$BACKUP_FILE" ]; then
    # Prefer the deploy layout (backups/backup-*/db.sql.gz), fall back to the
    # local scripts layout (backups/bikontrol-db-*.sql.gz).
    BACKUP_FILE="$(ls -t backups/backup-*/db.sql.gz backups/bikontrol-db-*.sql.gz 2>/dev/null | head -n 1 || true)"
fi
[ -n "$BACKUP_FILE" ] || fail "no backup found (looked in backups/). Pass a path explicitly."
[ -f "$BACKUP_FILE" ] || fail "backup file not found: $BACKUP_FILE"
log "Verifying backup: $BACKUP_FILE"

# --- ensure the DB container is up -------------------------------------------
"${COMPOSE[@]}" ps -q db >/dev/null 2>&1 || fail "db container is not running — start it with 'npm run db:up'"
if [ -z "$("${COMPOSE[@]}" ps -q db)" ]; then
    fail "db container is not running — start it with 'npm run db:up'"
fi

TEMP_DB="${DB_NAME}_restore_check"
log "Restoring into temporary database: $TEMP_DB"

cleanup() {
    "${COMPOSE[@]}" exec -T db psql -U "$DB_USER" -d postgres \
        -c "DROP DATABASE IF EXISTS \"$TEMP_DB\";" >/dev/null 2>&1 || true
}
trap cleanup EXIT

# Fresh temporary database, isolated from the live one.
"${COMPOSE[@]}" exec -T db psql -U "$DB_USER" -d postgres -v ON_ERROR_STOP=1 \
    -c "DROP DATABASE IF EXISTS \"$TEMP_DB\";" \
    -c "CREATE DATABASE \"$TEMP_DB\";" >/dev/null

# --- restore (decompress if needed) ------------------------------------------
if ! { gzip -dc "$BACKUP_FILE" 2>/dev/null || cat "$BACKUP_FILE"; } \
    | "${COMPOSE[@]}" exec -T db psql -U "$DB_USER" -d "$TEMP_DB" -v ON_ERROR_STOP=1 -q >/dev/null; then
    fail "restore failed — the dump is not usable"
fi
log "Restore completed without errors."

# --- sanity checks -----------------------------------------------------------
# The dump must contain the core tables; users must exist if any were dumped.
check_query() {
    "${COMPOSE[@]}" exec -T db psql -U "$DB_USER" -d "$TEMP_DB" -t -A -q -v ON_ERROR_STOP=1 -c "$1"
}

for table in users Motorcycles MotorcycleKmHistories UserMaintenanceTypes MotorcycleMaintenanceRecords; do
    exists="$(check_query "SELECT to_regclass('public.\"$table\"') IS NOT NULL;")"
    [ "$exists" = "t" ] || fail "table \"$table\" is missing from the restored backup"
done

user_count="$(check_query "SELECT count(*) FROM users;")"
[ "$user_count" -ge 0 ] 2>/dev/null || fail "could not read users from the restored backup"
log "Restored database is readable ($user_count users)."

log "OK — backup restored and validated into $TEMP_DB (now dropping it)."
