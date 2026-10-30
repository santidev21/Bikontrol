#!/bin/bash
set -euo pipefail

# Bikontrol Deployment Script
# Usage: ./scripts/deploy.sh [deploy [tag]|status|logs|verify|rollback [tag]|...]
#
# Two modes:
#  - Tagged (CI): `deploy sha-<commit>` pulls the images CI already built, scanned
#    and published to GHCR, checks out the matching commit for compose/scripts
#    and never builds on the server. `rollback` returns to the previous tag.
#  - Build (manual / staging): `deploy` without a tag builds on this host and
#    keeps the serving images as `:previous`.

DEPLOY_DIR="${DEPLOY_DIR:-/opt/bikontrol}"
LOG_FILE="${LOG_FILE:-/tmp/bikontrol-deploy.log}"
TIMESTAMP="$(date +%Y%m%d-%H%M%S)"
BACKUP_DIR="${DEPLOY_DIR}/backups/backup-${TIMESTAMP}"
BACKUP_RETENTION="${BACKUP_RETENTION:-14}"
# Offsite copy (optional but strongly recommended): an rclone remote path such as
# "b2:bikontrol-backups". Dumps are encrypted client-side with the key file below
# before leaving the host, so the storage provider never sees user data.
BACKUP_OFFSITE_REMOTE="${BACKUP_OFFSITE_REMOTE:-}"
BACKUP_ENCRYPTION_KEY_FILE="${BACKUP_ENCRYPTION_KEY_FILE:-${DEPLOY_DIR}/.backup-key}"
# Public base URL the post-deploy smoke test hits (frontend + API under one
# origin). Override for staging: SMOKE_URL=https://staging.example ./deploy.sh deploy
SMOKE_URL="${SMOKE_URL:-https://bikontrol.santidev21.tech}"

# Image state of the stack that is serving now (IMAGE_TAG, API_IMAGE, WEB_IMAGE,
# PREVIOUS_IMAGE_TAG). Sourced on every run so `up`, `migrate`, `status`… always
# address the same images compose was last started with.
STATE_FILE="${DEPLOY_DIR}/.deploy-state"
# The state file is the only source of truth for what is serving. Ignore any
# IMAGE_TAG / PREVIOUS_IMAGE_TAG inherited from the caller's environment: a CI
# step that exports the *new* tag would otherwise be recorded as the "previous"
# one on the first tagged deploy, and rollback would point at itself.
IMAGE_TAG=""
PREVIOUS_IMAGE_TAG=""
if [[ -f "$STATE_FILE" ]]; then
    # shellcheck disable=SC1090
    source "$STATE_FILE"
fi
export IMAGE_TAG="${IMAGE_TAG:-}" API_IMAGE="${API_IMAGE:-}" WEB_IMAGE="${WEB_IMAGE:-}"
[[ -n "$IMAGE_TAG" ]] || unset IMAGE_TAG
[[ -n "$API_IMAGE" ]] || unset API_IMAGE
[[ -n "$WEB_IMAGE" ]] || unset WEB_IMAGE

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" | tee -a "$LOG_FILE"
}

error_exit() {
    log "ERROR: $1"
    exit 1
}

validate_docker() {
    log "Checking Docker..."
    docker info >/dev/null 2>&1 || error_exit "Docker is not running or not accessible"
    log "Docker available."
}

validate_env() {
    log "Validating .env file..."
    if [[ ! -f "$DEPLOY_DIR/.env" ]]; then
        error_exit ".env file not found at $DEPLOY_DIR/.env (copy .env.example and fill it in)"
    fi
    if grep -q "CHANGE_ME" "$DEPLOY_DIR/.env"; then
        error_exit ".env still contains CHANGE_ME placeholders — set real values before deploying"
    fi
    log ".env validated."
}

validate_config() {
    log "Validating docker compose configuration..."
    (cd "$DEPLOY_DIR" && docker compose config --quiet) || error_exit "docker compose config validation failed"
    log "Configuration valid."
}

backup() {
    mkdir -p "$(dirname "$BACKUP_DIR")"
    if [[ -d "$DEPLOY_DIR" ]]; then
        log "Creating config backup at ${BACKUP_DIR}.tgz..."
        tar czf "${BACKUP_DIR}.tgz" -C / \
            --exclude=opt/bikontrol/.git \
            --exclude=opt/bikontrol/.env \
            --exclude=opt/bikontrol/backups \
            opt/bikontrol 2>/dev/null || true
        chmod 700 "${BACKUP_DIR}.tgz"
        log "Backup created."
    fi
}

backup_database() {
    if [[ ! -f "$DEPLOY_DIR/.env" ]]; then
        return 0
    fi
    set -a
    # shellcheck disable=SC1091
    source "$DEPLOY_DIR/.env"
    set +a

    mkdir -p "$(dirname "$BACKUP_DIR")"
    mkdir -p "$BACKUP_DIR"
    if docker compose -f "$DEPLOY_DIR/docker-compose.yml" ps -q db >/dev/null 2>&1; then
        log "Backing up database to ${BACKUP_DIR}/db.sql.gz ..."
        if ! docker compose -f "$DEPLOY_DIR/docker-compose.yml" exec -T db \
            pg_dump --clean --if-exists -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" 2>>"$LOG_FILE" | gzip > "${BACKUP_DIR}/db.sql.gz"; then
            error_exit "database backup failed — aborting"
        fi
        if ! gzip -t "${BACKUP_DIR}/db.sql.gz" 2>/dev/null; then
            error_exit "database backup looks corrupt (${BACKUP_DIR}/db.sql.gz) — aborting"
        fi
        # A gzip-valid dump can still be unrestorable. Restore it into a
        # throwaway database and check the core tables before trusting it.
        if ! "${DEPLOY_DIR}/scripts/db-verify-backup.sh" "${BACKUP_DIR}/db.sql.gz"; then
            error_exit "database backup failed verification — aborting"
        fi
        offsite_backup "${BACKUP_DIR}/db.sql.gz" || log "WARNING: offsite backup failed — only the local copy exists"
        # prune old backups, keep last N
        log "Pruning old backups (keeping last $BACKUP_RETENTION) ..."
        ls -dt "${DEPLOY_DIR}/backups"/backup-* 2>/dev/null | tail -n +$((BACKUP_RETENTION + 1)) | xargs -r rm -rf
    fi
}

# Encrypts a dump with the local key file and ships it with rclone. A no-op when
# BACKUP_OFFSITE_REMOTE is unset; fails loudly when it is set but unusable.
offsite_backup() {
    local dump="$1"
    if [[ -z "$BACKUP_OFFSITE_REMOTE" ]]; then
        log "Offsite backup not configured (BACKUP_OFFSITE_REMOTE unset) — the dump only exists on this host."
        return 0
    fi
    command -v rclone >/dev/null 2>&1 || { log "ERROR: rclone is not installed"; return 1; }
    command -v openssl >/dev/null 2>&1 || { log "ERROR: openssl is not installed"; return 1; }
    [[ -s "$BACKUP_ENCRYPTION_KEY_FILE" ]] || { log "ERROR: encryption key file missing or empty: ${BACKUP_ENCRYPTION_KEY_FILE}"; return 1; }

    local encrypted="${dump}.enc"
    local remote_name="backup-${TIMESTAMP}.sql.gz.enc"
    log "Encrypting and uploading the dump to ${BACKUP_OFFSITE_REMOTE}/${remote_name} ..."
    if ! openssl enc -aes-256-cbc -pbkdf2 -salt -in "$dump" -out "$encrypted" -pass "file:${BACKUP_ENCRYPTION_KEY_FILE}"; then
        rm -f "$encrypted"
        log "ERROR: encryption failed"
        return 1
    fi
    if ! rclone copyto "$encrypted" "${BACKUP_OFFSITE_REMOTE}/${remote_name}"; then
        rm -f "$encrypted"
        log "ERROR: upload failed"
        return 1
    fi
    rm -f "$encrypted"
    log "Offsite copy uploaded."
}

wait_db_ready() {
    local tries=0
    until docker compose -f "$DEPLOY_DIR/docker-compose.yml" exec -T db         pg_isready -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" >/dev/null 2>&1; do
        tries=$((tries + 1))
        [[ "$tries" -le 30 ]] || return 1
        sleep 2
    done
}

# Restores a dump taken by backup_database into the LIVE database. The `public`
# schema is dropped and recreated first, so objects created AFTER the dump (e.g.
# tables of a migration being rolled back) do not survive and the migrations
# history matches the restored schema. Everything runs in a single transaction
# with ON_ERROR_STOP: it either replaces the database entirely or leaves it
# untouched. Expects POSTGRES_* in the environment and the db running.
restore_dump() {
    local dump="$1"
    [[ -f "$dump" ]] || { log "ERROR: dump not found: ${dump}"; return 1; }
    local reader=(cat)
    [[ "$dump" == *.gz ]] && reader=(gunzip -c)
    {
        echo 'DROP SCHEMA IF EXISTS public CASCADE; CREATE SCHEMA public; GRANT ALL ON SCHEMA public TO PUBLIC;'
        "${reader[@]}" "$dump"
    } | docker compose -f "$DEPLOY_DIR/docker-compose.yml" exec -T db \
        psql -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" -v ON_ERROR_STOP=1 --single-transaction -q >/dev/null
}

# Disaster recovery entry point: verify the dump in a throwaway database first,
# then stop the app, restore, and start it again.
restore_db() {
    local dump="${2:-}"
    [[ -n "$dump" ]] || error_exit "usage: $0 restore-db <path/to/db.sql.gz>"
    [[ -f "$DEPLOY_DIR/.env" ]] || error_exit ".env not found at $DEPLOY_DIR/.env"
    set -a
    # shellcheck disable=SC1091
    source "$DEPLOY_DIR/.env"
    set +a

    log "=== Restoring ${dump} into ${POSTGRES_DB} ==="
    "${DEPLOY_DIR}/scripts/db-verify-backup.sh" "$dump" || error_exit "dump failed verification — refusing to restore"
    (cd "$DEPLOY_DIR" && docker compose stop api web worker) || true
    (cd "$DEPLOY_DIR" && docker compose up -d db)
    wait_db_ready || error_exit "database did not become ready"
    restore_dump "$dump" || error_exit "restore failed — the live database was left untouched (single transaction)"
    (cd "$DEPLOY_DIR" && docker compose up -d --remove-orphans)
    log "Restore complete. Run: ./scripts/deploy.sh smoke"
}

backup_only() {
    BACKUP_DIR="${DEPLOY_DIR}/backups/backup-${TIMESTAMP}"
    log "=== Manual backup: ${BACKUP_DIR} ==="
    mkdir -p "$BACKUP_DIR"
    backup_database
    log "Backup complete at ${BACKUP_DIR}"
    ls -lh "${BACKUP_DIR}/" 2>/dev/null || true
}

# Read-only data integrity audit (scripts/db-integrity-audit.sql).
# Every block must return 0 rows; any output aborts the deploy.
audit_database() {
    if [[ ! -f "$DEPLOY_DIR/.env" ]]; then
        return 0
    fi
    set -a
    # shellcheck disable=SC1091
    source "$DEPLOY_DIR/.env"
    set +a

    local audit_sql="${DEPLOY_DIR}/scripts/db-integrity-audit.sql"
    if [[ ! -f "$audit_sql" ]]; then
        log "WARNING: ${audit_sql} not found — skipping integrity audit"
        return 0
    fi
    if ! docker compose -f "$DEPLOY_DIR/docker-compose.yml" ps -q db >/dev/null 2>&1; then
        log "WARNING: db service not running — skipping integrity audit"
        return 0
    fi

    log "Running read-only integrity audit..."
    local findings
    findings="$(docker compose -f "$DEPLOY_DIR/docker-compose.yml" exec -T db \
        psql -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" -t -A -q -v ON_ERROR_STOP=1 -f - \
        < "$audit_sql" 2>/dev/null || true)"

    if [[ -n "$findings" ]]; then
        log "ERROR: integrity audit returned findings:"
        echo "$findings" | tee -a "$LOG_FILE"
        error_exit "integrity audit failed — fix the data before deploying"
    fi
    log "Integrity audit clean (0 rows)."
}

install_cron() {
    local schedule="0 3 * * *"
    local cmd="${DEPLOY_DIR}/scripts/deploy.sh backup-db >> /var/log/bikontrol-backup.log 2>&1"
    local cron_line="${schedule} ${cmd}"
    # Allow custom schedule: ./scripts/deploy.sh install-cron "0 3 * * 0"
    if [[ -n "${2:-}" ]]; then
        # second argument overrides schedule
        cron_line="${2} ${cmd}"
    fi
    log "Installing daily backup cron: ${cron_line}"
    # Ensure backup dir + log file exist
    mkdir -p "${DEPLOY_DIR}/backups"
    touch /var/log/bikontrol-backup.log 2>/dev/null || true
    # Remove any previous bikontrol backup crons to avoid duplicates
    (crontab -l 2>/dev/null | grep -v "bikontrol.*backup-db" || true; echo "$cron_line") | crontab -
    log "Cron installed. Current crontab:"
    crontab -l 2>/dev/null | grep bikontrol || true
    log "Backups run daily at 03:00 UTC. Change with: ./scripts/deploy.sh install-cron \"<cron>\""
}

remove_cron() {
    log "Removing bikontrol backup cron..."
    (crontab -l 2>/dev/null | grep -v "bikontrol.*backup-db" || true) | crontab -
    log "Cron removed."
}

# Persists the image state so later invocations (and rollback) know what runs.
write_state() {
    {
        echo "IMAGE_TAG=${IMAGE_TAG:-}"
        echo "API_IMAGE=${API_IMAGE:-}"
        echo "WEB_IMAGE=${WEB_IMAGE:-}"
        echo "PREVIOUS_IMAGE_TAG=${PREVIOUS_IMAGE_TAG:-}"
    } > "$STATE_FILE"
}

# `sha-<commit>` tags map to the commit whose compose file and scripts match the
# images, so a tagged deploy never mixes new images with newer config.
commit_of_tag() {
    case "$1" in
        sha-*) echo "${1#sha-}" ;;
        *) echo "" ;;
    esac
}

checkout_commit() {
    local commit="$1"
    [[ -n "$commit" ]] || return 0
    (cd "$DEPLOY_DIR" && git fetch origin main && git reset --hard "$commit")
}

# GHCR login for private packages. CI passes a short-lived GITHUB_TOKEN; manual
# runs rely on a previous `docker login`.
registry_login() {
    if [[ -n "${REGISTRY_TOKEN:-}" ]] && [[ -n "${REGISTRY_USER:-}" ]]; then
        echo "$REGISTRY_TOKEN" | docker login ghcr.io -u "$REGISTRY_USER" --password-stdin >/dev/null \
            || error_exit "registry login failed"
    fi
}

pull_images() {
    log "Pulling images tagged ${IMAGE_TAG} ..."
    registry_login
    (cd "$DEPLOY_DIR" && docker compose pull api web) || error_exit "could not pull images for tag ${IMAGE_TAG}"
}

pull() {
    log "Pulling latest code..."
    (cd "$DEPLOY_DIR" && git fetch origin main && git reset --hard origin/main)
    log "Code updated."
}

# Keeps the images that are serving traffic now as `:previous`, so a failed
# deploy can roll back to the exact previous build (the build below is
# `--no-cache` and overwrites `:latest`).
tag_previous_images() {
    local image
    for image in bikontrol-api bikontrol-web; do
        if docker image inspect "${image}:latest" >/dev/null 2>&1; then
            docker tag "${image}:latest" "${image}:previous"
            log "Kept ${image}:latest as ${image}:previous."
        else
            log "No ${image}:latest image to keep (first deploy with image tags) — rollback will rebuild."
        fi
    done
}

previous_images_available() {
    docker image inspect bikontrol-api:previous >/dev/null 2>&1 \
        && docker image inspect bikontrol-web:previous >/dev/null 2>&1
}

build() {
    if [[ -n "${DEPLOY_TAGGED:-}" ]]; then
        pull_images
        return 0
    fi
    tag_previous_images
    log "Building images..."
    (cd "$DEPLOY_DIR" && docker compose build --no-cache)
    log "Build complete."
}

# Apply EF Core migrations as a dedicated, one-shot step — before the API
# starts and without racing replicas. Uses the freshly built API image in
# migration-only mode (`--migrate`), on the internal network so it can reach
# the database. Idempotent: already-applied migrations are skipped.
migrate() {
    log "Applying database migrations (dedicated step)..."
    # The DB must be reachable; start it (idempotent) and give it a moment. The
    # api service depends on a healthy db, but `run` starts on demand.
    set -a
    # shellcheck disable=SC1091
    source "$DEPLOY_DIR/.env"
    set +a
    (cd "$DEPLOY_DIR" && docker compose up -d db)
    wait_db_ready || error_exit "database did not become ready — aborting (nothing was started)"
    (cd "$DEPLOY_DIR" && \
        docker compose run --rm --no-deps api --migrate) \
        || error_exit "database migration failed — aborting (nothing was started)"
    log "Migrations applied."
}

# One-off after enabling object storage: moves attachment images that still
# live as data URLs in the database into the bucket. Idempotent and resumable
# (run it again after a failure); the app keeps serving both kinds meanwhile.
migrate_images() {
    log "Moving database-stored images (attachments and motorcycle photos) to object storage..."
    set -a
    # shellcheck disable=SC1091
    source "$DEPLOY_DIR/.env"
    set +a
    [[ "${Storage__Enabled:-false}" == "true" ]] || error_exit "Storage__Enabled is not true in $DEPLOY_DIR/.env — nothing to migrate to"
    (cd "$DEPLOY_DIR" && docker compose up -d db)
    wait_db_ready || error_exit "database did not become ready"
    (cd "$DEPLOY_DIR" && docker compose run --rm --no-deps api --migrate-images) \
        || error_exit "image migration reported failures — check the log and run it again"
    log "Image migration finished."
}

up() {
    log "Starting containers..."
    (cd "$DEPLOY_DIR" && docker compose up -d --remove-orphans)
    log "Containers started."
}

# Health of the (unnamed) worker container; "not_found" when it is not running.
worker_status() {
    local id
    id="$(cd "$DEPLOY_DIR" && docker compose ps -q worker 2>/dev/null || true)"
    if [[ -z "$id" ]]; then
        echo "not_found"
        return
    fi
    docker inspect --format='{{.State.Health.Status}}' "$id" 2>/dev/null || echo "not_found"
}

wait_healthy() {
    log "Waiting for services to become healthy..."
    local TIMEOUT=180
    local INTERVAL=5
    local ELAPSED=0

    while [[ $ELAPSED -lt $TIMEOUT ]]; do
        local API_STATUS WEB_STATUS WORKER_STATUS
        API_STATUS=$(docker inspect --format='{{.State.Health.Status}}' bikontrol-api 2>/dev/null || echo "not_found")
        WEB_STATUS=$(docker inspect --format='{{.State.Health.Status}}' bikontrol 2>/dev/null || echo "not_found")
        WORKER_STATUS=$(worker_status)

        if [[ "$API_STATUS" = "healthy" ]] && [[ "$WEB_STATUS" = "healthy" ]] && [[ "$WORKER_STATUS" = "healthy" ]]; then
            log "Services healthy."
            return 0
        fi

        log "Waiting... ($ELAPSED/$TIMEOUT) api: $API_STATUS web: $WEB_STATUS worker: $WORKER_STATUS"
        sleep $INTERVAL
        ELAPSED=$((ELAPSED + INTERVAL))
    done

    (cd "$DEPLOY_DIR" && docker compose ps && docker compose logs --tail=50)
    error_exit "Health check timeout"
}

verify() {
    log "Verifying deployment..."
    local API_STATUS WEB_STATUS WORKER_STATUS
    API_STATUS=$(docker inspect --format='{{.State.Health.Status}}' bikontrol-api 2>/dev/null || echo "not_found")
    WEB_STATUS=$(docker inspect --format='{{.State.Health.Status}}' bikontrol 2>/dev/null || echo "not_found")
    WORKER_STATUS=$(worker_status)

    [[ "$WORKER_STATUS" = "healthy" ]] || error_exit "worker is not healthy (status: $WORKER_STATUS)"
    [[ "$API_STATUS" = "healthy" ]] || error_exit "bikontrol-api is not healthy (status: $API_STATUS)"
    [[ "$WEB_STATUS" = "healthy" ]] || error_exit "bikontrol is not healthy (status: $WEB_STATUS)"

    log "Deployment verified (containers healthy)."
}

# End-to-end smoke test against the public origin. Catches regressions that the
# container healthcheck cannot (e.g. the gateway not routing, a broken build
# being served, the SPA shell not loading).
smoke() {
    local script="${DEPLOY_DIR}/scripts/smoke-test.sh"
    if [[ ! -f "$script" ]]; then
        log "WARNING: ${script} not found — skipping smoke test"
        return 0
    fi
    log "Running post-deploy smoke test against ${SMOKE_URL} ..."
    if ! SMOKE_TIMEOUT="${SMOKE_TIMEOUT:-10}" "$script" "$SMOKE_URL"; then
        error_exit "smoke test failed against ${SMOKE_URL}"
    fi
    log "Smoke test passed."
}

rollback() {
    log "Rolling back..."
    local failed=0
    (cd "$DEPLOY_DIR" && docker compose stop api web worker) || true

    # 1) Database: restore the dump taken at the start of this deploy.
    local dump=""
    if [[ -f "${BACKUP_DIR}/db.sql.gz" ]]; then dump="${BACKUP_DIR}/db.sql.gz"
    elif [[ -f "${BACKUP_DIR}/db.sql" ]]; then dump="${BACKUP_DIR}/db.sql"
    fi
    if [[ -n "$dump" ]] && [[ -f "$DEPLOY_DIR/.env" ]]; then
        set -a
        # shellcheck disable=SC1091
        source "$DEPLOY_DIR/.env"
        set +a
        log "Starting db to restore dump..."
        (cd "$DEPLOY_DIR" && docker compose up -d db)
        if ! wait_db_ready; then
            log "ERROR: database did not become ready — cannot restore"
            failed=1
        elif ! restore_dump "$dump"; then
            # Single transaction: a failed restore leaves the database as it was.
            log "ERROR: database restore failed — the database was left as it was"
            failed=1
        else
            log "Database restored from ${dump}."
        fi
    else
        log "WARNING: no pre-deploy dump found in ${BACKUP_DIR} — the database was NOT restored"
    fi

    # 2) Code: go back to the previous images (no rebuild, no source needed).
    local target_tag="${ROLLBACK_TAG:-${PREVIOUS_IMAGE_TAG:-}}"
    if [[ -n "$target_tag" ]]; then
        log "Rolling back images to tag ${target_tag} ..."
        checkout_commit "$(commit_of_tag "$target_tag")" || log "WARNING: could not check out the commit for ${target_tag}"
        PREVIOUS_IMAGE_TAG="${IMAGE_TAG:-}"
        export IMAGE_TAG="$target_tag"
        registry_login
        (cd "$DEPLOY_DIR" && docker compose pull api web) || log "WARNING: pull failed — using local images if present"
        (cd "$DEPLOY_DIR" && docker compose up -d --no-build --remove-orphans)
        write_state
    elif previous_images_available; then
        log "Restoring previous images..."
        unset IMAGE_TAG API_IMAGE WEB_IMAGE
        write_state
        docker tag bikontrol-api:previous bikontrol-api:latest
        docker tag bikontrol-web:previous bikontrol-web:latest
        (cd "$DEPLOY_DIR" && docker compose up -d --no-build --remove-orphans)
    else
        # No kept images (first deploy with image tags): fall back to the config
        # backup and rebuild from it.
        unset IMAGE_TAG API_IMAGE WEB_IMAGE
        write_state
        if [[ -f "${BACKUP_DIR}.tgz" ]]; then
            log "No previous images — restoring the config backup and rebuilding..."
            tar xzf "${BACKUP_DIR}.tgz" -C /
        fi
        (cd "$DEPLOY_DIR" && docker compose up -d --build --remove-orphans)
    fi
    log "Rollback finished."

    # Best-effort: confirm the restored stack actually serves (do not fail the
    # rollback itself if the smoke test has a transient hiccup).
    if [[ -f "${DEPLOY_DIR}/scripts/smoke-test.sh" ]]; then
        log "Verifying rollback (best effort) ..."
        SMOKE_TIMEOUT="${SMOKE_TIMEOUT:-10}" "${DEPLOY_DIR}/scripts/smoke-test.sh" "$SMOKE_URL" \
            || log "WARNING: smoke test after rollback did not pass — investigate"
    fi

    if [[ "$failed" -ne 0 ]]; then
        log "ERROR: rollback finished WITH ERRORS — check the log above and docs/RUNBOOK.md §4"
        return 1
    fi
}

deploy() {
    local tag="${1:-}"
    local from_tag="${IMAGE_TAG:-}"
    if [[ -n "$tag" ]]; then
        DEPLOY_TAGGED=1
        PREVIOUS_IMAGE_TAG="$from_tag"
        export IMAGE_TAG="$tag"
        : "${API_IMAGE:?API_IMAGE (e.g. ghcr.io/<owner>/bikontrol-api) is required for a tagged deploy}"
        : "${WEB_IMAGE:?WEB_IMAGE (e.g. ghcr.io/<owner>/bikontrol-web) is required for a tagged deploy}"
        export API_IMAGE WEB_IMAGE
        log "=== Starting Bikontrol deployment of ${tag} (previous: ${from_tag:-none}) ==="
    else
        log "=== Starting Bikontrol deployment (build on host) ==="
        # Build mode owns the plain local image names; forget any tagged state.
        unset IMAGE_TAG API_IMAGE WEB_IMAGE PREVIOUS_IMAGE_TAG
    fi
    validate_docker
    validate_env
    validate_config
    backup
    backup_database
    audit_database
    if [[ -n "$tag" ]]; then
        checkout_commit "$(commit_of_tag "$tag")"
    else
        pull
    fi
    build
    migrate
    up
    if ! wait_healthy; then
        log "Deployment failed health check — rolling back."
        if rollback; then
            error_exit "Deployment failed and was rolled back"
        fi
        error_exit "Deployment failed and the ROLLBACK ALSO FAILED — manual recovery needed (docs/RUNBOOK.md §4)"
    fi
    verify
    # A green healthcheck is not enough: prove the public surface actually works
    # end-to-end, and roll back if it does not.
    if ! smoke; then
        log "Deployment failed smoke test — rolling back."
        if rollback; then
            error_exit "Deployment failed the smoke test and was rolled back"
        fi
        error_exit "Deployment failed the smoke test and the ROLLBACK ALSO FAILED — manual recovery needed (docs/RUNBOOK.md §4)"
    fi
    write_state
    log "=== Deployment successful ==="
    status
}

status() {
    (cd "$DEPLOY_DIR" && docker compose ps)
    echo ""
    (cd "$DEPLOY_DIR" && docker compose logs --tail=20)
}

logs() {
    (cd "$DEPLOY_DIR" && docker compose logs --tail=100)
}

main() {
    case "${1:-deploy}" in
        pull) pull ;;
        build) build ;;
        migrate) migrate ;;
        migrate-images|migrate-attachments) migrate_images ;;
        up) up ;;
        deploy) deploy "${2:-}" ;;
        status) status ;;
        logs) logs ;;
        verify) verify ;;
        smoke) smoke ;;
        rollback) ROLLBACK_TAG="${2:-}"; rollback ;;
        restore-db) restore_db "$@" ;;
        backup|backup-db|backup-only) backup_only ;;
        audit-db) audit_database ;;
        install-cron) install_cron "$@" ;;
        remove-cron) remove_cron ;;
        *)
            echo "Usage: $0 [pull|build|migrate|migrate-images|up|deploy|status|logs|verify|smoke|rollback|restore-db|backup-db|audit-db|install-cron|remove-cron]"
            exit 1
            ;;
    esac
}

# One line: bash has parsed all of main before executing it, so the git reset in
# `pull`/`checkout_commit` can rewrite this file mid-run without corrupting it.
main "$@"; exit $?
