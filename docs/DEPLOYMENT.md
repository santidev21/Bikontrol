# Bikontrol — Production Deployment

Bikontrol is served at `https://bikontrol.santidev21.tech/` behind the `vps-gateway` reverse proxy. Deploys happen automatically on push to `main` via GitHub Actions.

## Architecture

```
Internet → gateway (nginx) → bikontrol (Angular, :80)
                          → bikontrol-api (.NET, :8080) → bikontrol-db (PostgreSQL)
```

- `bikontrol-net` (external, shared with the gateway): `bikontrol` + `bikontrol-api`.
- `bikontrol-internal-net` (internal): database only. The DB is **never** on the shared network.

## One-time VPS setup

```bash
git clone <repo> /opt/bikontrol
cd /opt/bikontrol
cp .env.example .env   # fill in real values, no CHANGE_ME left
# create the shared networks (or let the gateway's init-networks.sh do it)
docker network create bikontrol-net
docker network create bikontrol-internal-net
```

## Deploy

Automatic on push to `main` via GitHub Actions, or manual:

```bash
cd /opt/bikontrol && ./scripts/deploy.sh deploy
```

## Gateway integration

1. Create DNS A record `bikontrol.santidev21.tech` → VPS IP.
2. Issue the certificate (see `vps-gateway/AGENTS.md`).
3. Copy `deploy/bikontrol.santidev21.tech.conf` into the gateway's `sites-enabled/` and add `bikontrol-net` to the gateway `docker-compose.yml` networks and `init-networks.sh`.
4. Reload: `docker exec gateway nginx -t && docker exec gateway nginx -s reload`.

## Database backups

Every `deploy` call already creates a compressed DB dump (`pg_dump` → `backups/backup-<timestamp>/db.sql.gz`) before pulling new code. Backups are kept in persistent `backups/` (not `/tmp`) with 7-copy retention.

Manual backup / restore (VPS or local):

```bash
# VPS manual backup (persistent)
./scripts/deploy.sh backup-db   # = ls /opt/bikontrol/backups

# Local (works with loopback DB on 5434)
npm run db:backup              # → backups/bikontrol-db-<ts>.sql.gz
npm run db:restore -- backups/bikontrol-db-xxx.sql.gz
```

**Automatic weekly backups** (recommended, VPS cron — Sunday 02:00 UTC):

```bash
# One-time: install the weekly cron (idempotent, replaces any previous bikontrol backup cron)
sudo /opt/bikontrol/scripts/deploy.sh install-cron
# Custom schedule: sudo /opt/bikontrol/scripts/deploy.sh install-cron "0 3 * * 0"
# Remove: sudo /opt/bikontrol/scripts/deploy.sh remove-cron
# Check: crontab -l | grep bikontrol
```

Manual cron line (equivalent):

```cron
0 2 * * 0 /opt/bikontrol/scripts/deploy.sh backup-db >> /var/log/bikontrol-backup.log 2>&1
```

Add offsite copy if desired: `rclone copy /opt/bikontrol/backups remote:backups/bikontrol` or `rsync`.

Backups are **semanales automáticos** vía cron; cada `deploy` también genera un backup justo antes de actualizar.

Recovery: `gunzip -c backups/backup-xxx/db.sql.gz | docker compose exec -T db psql -U "$POSTGRES_USER" -d "$POSTGRES_DB"` (done by `./scripts/deploy.sh rollback` on failed deploy).

## Database TLS

Postgres runs with `ssl=on` via `docker/db/init-ssl.sh` (self-signed `CN=bikontrol-db`, certs in the data volume, `ssl=Require` on the server). All connection strings carry `SslMode=Require;Trust Server Certificate=true` (self-signed). The DB remains on `bikontrol-internal-net` only; no host port is published in production (`docker-compose.local.yml` publishes `127.0.0.1:5434` only for local dev).

## Required secrets (`.env`, never committed)

| Variable | Purpose |
|---|---|
| `POSTGRES_DB` / `POSTGRES_USER` / `POSTGRES_PASSWORD` | Database credentials |
| `ConnectionStrings__DefaultConnection` | Full Npgsql connection string (**must include `SslMode=Require;Trust Server Certificate=true`** for TLS) |
| `Jwt__Key` | JWT signing key (generate with `openssl rand -base64 48`) |
| `Jwt__Issuer` / `Jwt__Audience` | JWT issuer/audience (defaults provided) |
| `Jwt__ExpireMinutes` / `Jwt__RefreshExpireDays` | Token lifetimes (defaults: 15 min / 30 days) |
| `Google__ClientId` | Google OAuth client ID (**required** for Sign in with Google; public value) |
| `DemoUser__Email` / `DemoUser__FullName` | Demo user identity (defaults `demo@bikontrol.com` / `Usuario Demo`) |
| `Frontend__BaseUrl` | Base URL for password-reset links (default `https://bikontrol.santidev21.tech`) |
| `Smtp__Host` / `Smtp__Port` / `Smtp__Username` / `Smtp__Password` / `Smtp__FromEmail` (+ `Smtp__FromName`, `Smtp__EnableSsl`) | SMTP for recovery emails (**required** in prod, otherwise reset links are only logged) |
| `Cors__AllowedOrigins` | Comma-separated browser origins |
