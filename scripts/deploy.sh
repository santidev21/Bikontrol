#!/bin/bash
set -euo pipefail

# Bikontrol Deployment Script
# Usage: ./scripts/deploy.sh [deploy|status|logs|verify|rollback]

DEPLOY_DIR="/opt/bikontrol"
LOG_FILE="/tmp/bikontrol-deploy.log"
TIMESTAMP="$(date +%Y%m%d-%H%M%S)"
BACKUP_DIR="/tmp/bikontrol-backup-${TIMESTAMP}"

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
    if [ -d "$DEPLOY_DIR" ]; then
        log "Creating config backup at ${BACKUP_DIR}.tgz..."
        tar czf "${BACKUP_DIR}.tgz" -C / \
            --exclude=opt/bikontrol/.git \
            --exclude=opt/bikontrol/.env \
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

    if docker compose -f "$DEPLOY_DIR/docker-compose.yml" ps -q db >/dev/null 2>&1; then
        log "Backing up database..."
        docker compose -f "$DEPLOY_DIR/docker-compose.yml" exec -T db \
            pg_dump -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" > "${BACKUP_DIR}/db.sql" \
            || log "WARNING: database backup failed (continuing)"
    fi
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

    if [ -f "${BACKUP_DIR}/db.sql" ] && [ -f "$DEPLOY_DIR/.env" ]; then
        set -a
        # shellcheck disable=SC1091
        source "$DEPLOY_DIR/.env"
        set +a
        log "Starting db to restore dump..."
        (cd "$DEPLOY_DIR" && docker compose up -d db)
        sleep 10
        docker compose -f "$DEPLOY_DIR/docker-compose.yml" exec -T db \
            psql -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" < "${BACKUP_DIR}/db.sql" \
            || log "WARNING: database restore failed"
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
    *)
        echo "Usage: $0 [pull|build|up|deploy|status|logs|verify|rollback]"
        exit 1
        ;;
esac
