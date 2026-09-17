#!/bin/bash
set -euo pipefail

# Bikontrol Deployment Script
# Usage: ./scripts/deploy.sh [deploy|status|logs|verify|rollback]

DEPLOY_DIR="/opt/bikontrol"
LOG_FILE="/tmp/bikontrol-deploy.log"
TIMESTAMP="$(date +%Y%m%d-%H%M%S)"
BACKUP_DIR="${DEPLOY_DIR}/backups/backup-${TIMESTAMP}"
BACKUP_RETENTION=7

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
    if [ ! -f "$DEPLOY_DIR/.env" ]; then
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
    if [ -d "$DEPLOY_DIR" ]; then
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
    if [ ! -f "$DEPLOY_DIR/.env" ]; then
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
            pg_dump -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" 2>/dev/null | gzip > "${BACKUP_DIR}/db.sql.gz"; then
            error_exit "database backup failed — aborting"
        fi
        if ! gzip -t "${BACKUP_DIR}/db.sql.gz" 2>/dev/null; then
            error_exit "database backup looks corrupt (${BACKUP_DIR}/db.sql.gz) — aborting"
        fi
        # prune old backups, keep last N
        log "Pruning old backups (keeping last $BACKUP_RETENTION) ..."
        ls -dt "${DEPLOY_DIR}/backups"/backup-* 2>/dev/null | tail -n +$((BACKUP_RETENTION + 1)) | xargs -r rm -rf
        ls -t "${DEPLOY_DIR}/backups"/backup-*/db.sql.gz 2>/dev/null | tail -n +$((BACKUP_RETENTION + 1)) | xargs -r rm -f || true
    fi
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
    if [ ! -f "$DEPLOY_DIR/.env" ]; then
        return 0
    fi
    set -a
    # shellcheck disable=SC1091
    source "$DEPLOY_DIR/.env"
    set +a

    local audit_sql="${DEPLOY_DIR}/scripts/db-integrity-audit.sql"
    if [ ! -f "$audit_sql" ]; then
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

    if [ -n "$findings" ]; then
        log "ERROR: integrity audit returned findings:"
        echo "$findings" | tee -a "$LOG_FILE"
        error_exit "integrity audit failed — fix the data before deploying"
    fi
    log "Integrity audit clean (0 rows)."
}

install_cron() {
    local schedule="0 2 * * 0"
    local cmd="${DEPLOY_DIR}/scripts/deploy.sh backup-db >> /var/log/bikontrol-backup.log 2>&1"
    local cron_line="${schedule} ${cmd}"
    # Allow custom schedule: ./scripts/deploy.sh install-cron "0 3 * * 0"
    if [ -n "${2:-}" ]; then
        # second argument overrides schedule
        cron_line="${2} ${cmd}"
    fi
    log "Installing weekly backup cron: ${cron_line}"
    # Ensure backup dir + log file exist
    mkdir -p "${DEPLOY_DIR}/backups"
    touch /var/log/bikontrol-backup.log 2>/dev/null || true
    # Remove any previous bikontrol backup crons to avoid duplicates
    (crontab -l 2>/dev/null | grep -v "bikontrol.*backup-db" || true; echo "$cron_line") | crontab -
    log "Cron installed. Current crontab:"
    crontab -l 2>/dev/null | grep bikontrol || true
    log "Backups run weekly on Sunday 02:00 UTC. Change with: ./scripts/deploy.sh install-cron \"<cron>\""
}

remove_cron() {
    log "Removing bikontrol backup cron..."
    (crontab -l 2>/dev/null | grep -v "bikontrol.*backup-db" || true) | crontab -
    log "Cron removed."
}

pull() {
    log "Pulling latest code..."
    (cd "$DEPLOY_DIR" && git fetch origin main && git reset --hard origin/main)
    log "Code updated."
}

build() {
    log "Building images..."
    (cd "$DEPLOY_DIR" && docker compose build --no-cache)
    log "Build complete."
}

up() {
    log "Starting containers..."
    (cd "$DEPLOY_DIR" && docker compose up -d --remove-orphans)
    log "Containers started."
}

wait_healthy() {
    log "Waiting for services to become healthy..."
    local TIMEOUT=180
    local INTERVAL=5
    local ELAPSED=0

    while [ $ELAPSED -lt $TIMEOUT ]; do
        local API_STATUS WEB_STATUS
        API_STATUS=$(docker inspect --format='{{.State.Health.Status}}' bikontrol-api 2>/dev/null || echo "not_found")
        WEB_STATUS=$(docker inspect --format='{{.State.Health.Status}}' bikontrol 2>/dev/null || echo "not_found")

        if [ "$API_STATUS" = "healthy" ] && [ "$WEB_STATUS" = "healthy" ]; then
            log "Services healthy."
            return 0
        fi

        log "Waiting... ($ELAPSED/$TIMEOUT) api: $API_STATUS web: $WEB_STATUS"
        sleep $INTERVAL
        ELAPSED=$((ELAPSED + INTERVAL))
    done

    (cd "$DEPLOY_DIR" && docker compose ps && docker compose logs --tail=50)
    error_exit "Health check timeout"
}

verify() {
    log "Verifying deployment..."
    local API_STATUS WEB_STATUS
    API_STATUS=$(docker inspect --format='{{.State.Health.Status}}' bikontrol-api 2>/dev/null || echo "not_found")
    WEB_STATUS=$(docker inspect --format='{{.State.Health.Status}}' bikontrol 2>/dev/null || echo "not_found")

    [ "$API_STATUS" = "healthy" ] || error_exit "bikontrol-api is not healthy (status: $API_STATUS)"
    [ "$WEB_STATUS" = "healthy" ] || error_exit "bikontrol is not healthy (status: $WEB_STATUS)"

    log "Deployment verified."
}

rollback() {
    log "Rolling back..."
    (cd "$DEPLOY_DIR" && docker compose down) || true

    # Support both .sql and .sql.gz backups
    local _dump=""
    if [ -f "${BACKUP_DIR}/db.sql.gz" ]; then _dump="${BACKUP_DIR}/db.sql.gz"
    elif [ -f "${BACKUP_DIR}/db.sql" ]; then _dump="${BACKUP_DIR}/db.sql"
    fi
    if [ -n "$_dump" ] && [ -f "$DEPLOY_DIR/.env" ]; then
        set -a
        # shellcheck disable=SC1091
        source "$DEPLOY_DIR/.env"
        set +a
        log "Starting db to restore dump..."
        (cd "$DEPLOY_DIR" && docker compose up -d db)
        sleep 10
        if [[ "$_dump" == *.gz ]]; then
            gunzip -c "$_dump" | docker compose -f "$DEPLOY_DIR/docker-compose.yml" exec -T db \
                psql -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" \
                || log "WARNING: database restore failed"
        else
            docker compose -f "$DEPLOY_DIR/docker-compose.yml" exec -T db \
                psql -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" < "$_dump" \
                || log "WARNING: database restore failed"
        fi
    fi

    if [ -f "${BACKUP_DIR}.tgz" ]; then
        log "Restoring config backup..."
        tar xzf "${BACKUP_DIR}.tgz" -C /
    fi

    (cd "$DEPLOY_DIR" && docker compose up -d --remove-orphans)
    log "Rollback complete."
}

deploy() {
    log "=== Starting Bikontrol deployment ==="
    validate_docker
    validate_env
    validate_config
    backup
    backup_database
    audit_database
    pull
    build
    up
    if ! wait_healthy; then
        log "Deployment failed health check — rolling back."
        rollback
        error_exit "Deployment failed and was rolled back"
    fi
    verify
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

case "${1:-deploy}" in
    pull) pull ;;
    build) build ;;
    up) up ;;
    deploy) deploy ;;
    status) status ;;
    logs) logs ;;
    verify) verify ;;
    rollback) rollback ;;
    backup|backup-db|backup-only) backup_only ;;
    audit-db) audit_database ;;
    install-cron) install_cron "$@" ;;
    remove-cron) remove_cron ;;
    *)
        echo "Usage: $0 [pull|build|up|deploy|status|logs|verify|rollback|backup-db|audit-db|install-cron|remove-cron]"
        exit 1
        ;;
esac
