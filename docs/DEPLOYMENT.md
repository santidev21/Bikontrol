# Bikontrol — Production Deployment

Bikontrol is served at `https://bikontrol.santidev21.tech/` behind the `vps-gateway` reverse proxy. Deploys happen automatically on push to `main` via GitHub Actions.

## Architecture

```
Internet → gateway (nginx) → bikontrol (Angular, :8080)
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

Each deploy runs, in order: backup + verify → integrity audit → pull → build → up → **container healthcheck** → **post-deploy smoke test**. If the healthcheck **or** the smoke test fails, the deploy **rolls back automatically** (restores the pre-deploy DB dump + config and brings the stack back up).

The smoke test (`scripts/smoke-test.sh`, also `npm run smoke <url>`) hits the public origin and asserts `/health`, `/ready` (DB), the SPA shell and the auth endpoints — read-only, no credentials.

### Staging

The same compose files and script serve a staging environment; point it at a separate deploy dir and host:

```bash
# On the staging VPS / directory:
DEPLOY_DIR=/opt/bikontrol-staging \
SMOKE_URL=https://staging.bikontrol.santidev21.tech \
  ./scripts/deploy.sh deploy
```

`DEPLOY_DIR`, `SMOKE_URL`, `LOG_FILE` and `BACKUP_RETENTION` are all environment-overridable, so staging shares the code but keeps its own data, backups and domain. Add a matching gateway site config (see the gateway repo) and a `.env` with staging values.

## Gateway integration

1. Create DNS A record `bikontrol.santidev21.tech` → VPS IP.
2. Issue the certificate (see `vps-gateway/AGENTS.md`).
3. Copy `deploy/bikontrol.santidev21.tech.conf` into the gateway's `sites-enabled/` and add `bikontrol-net` to the gateway `docker-compose.yml` networks and `init-networks.sh`.
4. Reload: `docker exec gateway nginx -t && docker exec gateway nginx -s reload`.

### HTTPS & security headers

- **TLS** terminates at the gateway with TLS 1.2/1.3 only, HSTS (`max-age=31536000; includeSubDomains; preload`), OCSP stapling and a strong cipher suite (gateway `snippets/ssl-params.conf` + `security-headers.conf`).
- **Security headers** (HSTS, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, COOP/CORP) are applied at the gateway for every response; the frontend container repeats the essential ones for defense in depth.
- **Content-Security-Policy** is set per site at the gateway (`sites-enabled/bikontrol.santidev21.tech.conf`) and mirrored in the frontend container (`docker/nginx.conf`). The allow-list is intentionally tight: `'self'` plus Google Identity Services (`accounts.google.com`) and the Font Awesome CDN (`cdnjs.cloudflare.com`); `data:`/`blob:` for resized photos. There is **no `'unsafe-eval'`**. When adding a third-party script/style/font/frame, add its origin to the CSP in **both** places or it will be blocked.

## Database backups

Every `deploy` call already creates a compressed DB dump (`pg_dump` → `backups/backup-<timestamp>/db.sql.gz`) before pulling new code. Backups are kept in persistent `backups/` (not `/tmp`) with 7-copy retention.

Manual backup / restore (VPS or local):

```bash
# VPS manual backup (persistent)
./scripts/deploy.sh backup-db   # = ls /opt/bikontrol/backups

# Local (works with loopback DB on 5434)
npm run db:backup              # → backups/bikontrol-db-<ts>.sql.gz
npm run db:verify-backup       # restore the newest backup into a throwaway DB + check it
npm run db:restore -- backups/bikontrol-db-xxx.sql.gz
```

**Verified backups.** A dump is only trusted after it restores. `scripts/db-verify-backup.sh` restores a backup into a temporary `*_restore_check` database inside the running `db` container, asserts the core tables exist and are readable, then drops it — it never touches the live database. `deploy` runs it right after `pg_dump` (a dump that does not restore aborts the deploy), and it can be run on any backup: `./scripts/db-verify-backup.sh backups/backup-XXXX/db.sql.gz`.

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

## Observability

- **Structured logs (Serilog)** go to stdout as JSON in production (readable text in Development), so `docker compose logs`/the gateway can collect them. Every log line carries a `TraceId`, and each request is logged once (`HTTP {Method} {Path} responded {StatusCode} in {Elapsed} ms`).
- **Request correlation:** errors echo an `X-Request-Id` header; the same id appears in the log line, so a user report maps to the exact log entry.
- **Error tracking (Sentry):** set `Sentry__Dsn` to capture unhandled exceptions with environment, release and trace context. It is a no-op when the DSN is empty (local dev/CI). PII is disabled (`SendDefaultPii=false`).
- **Uptime monitoring:** point an external monitor (UptimeRobot/Better Stack/`healthchecks.io`, or the gateway) at `GET /ready`; it returns `503` when PostgreSQL is unreachable, so an alert fires before users notice.

## Database TLS

Postgres runs with `ssl=on` via `docker/db/init-ssl.sh` (self-signed `CN=bikontrol-db`, certs in `/etc/postgresql/ssl` inside the container — never in the data volume, which must stay empty for `initdb`; `ssl=Require` on the server). All connection strings carry `SslMode=Require;Trust Server Certificate=true` (self-signed). The DB remains on `bikontrol-internal-net` only; no host port is published in production (`docker-compose.local.yml` publishes `127.0.0.1:5434` only for local dev).

## Required secrets (`.env`, never committed)

| Variable | Purpose |
|---|---|
| `POSTGRES_DB` / `POSTGRES_USER` / `POSTGRES_PASSWORD` | Database credentials |
| `ConnectionStrings__DefaultConnection` | Full Npgsql connection string (**must include `SslMode=Require;Trust Server Certificate=true`** for TLS) |
| `Jwt__Key` | JWT signing key (generate with `openssl rand -base64 48`) |
| `Jwt__Issuer` / `Jwt__Audience` | JWT issuer/audience (defaults provided) |
| `Jwt__ExpireMinutes` / `Jwt__RefreshExpireDays` | Token lifetimes (defaults: 15 min / 30 days) |
| `Google__ClientId` | Google OAuth client ID (**required** for Sign in with Google; public value) |
| `Demo__Enabled` | Public demo tenant (default `false`). When off, `POST /api/auth/demo` returns `404` and no demo data is seeded. Enable only for a deliberate public demo (also set `environment.demoEnabled` in the web build). |
| `EmailConfirmation__Required` | Require email confirmation before login (default `true`). Requires working SMTP; set `false` to skip verification. |
| `Lockout__Enabled` / `Lockout__MaxFailedAttempts` / `Lockout__Minutes` | Account lockout after repeated failed logins (defaults: `true` / `5` / `15`). |
| `Sentry__Dsn` / `Sentry__TracesSampleRate` | Error tracking (Sentry). Empty DSN = disabled. Sample rate default `0.1`. |
| `DemoUser__Email` / `DemoUser__FullName` | Demo user identity (defaults `demo@bikontrol.com` / `Usuario Demo`); only used when `Demo__Enabled=true` |
| `Frontend__BaseUrl` | Base URL for password-reset links (default `https://bikontrol.santidev21.tech`) |
| `Smtp__Host` / `Smtp__Port` / `Smtp__Username` / `Smtp__Password` / `Smtp__FromEmail` (+ `Smtp__FromName`, `Smtp__EnableSsl`) | SMTP for recovery emails (**required** in prod, otherwise reset links are only logged) |
| `Cors__AllowedOrigins` | Comma-separated browser origins |

### Secret management & rotation

- **Never in the repo.** Real values live in `/opt/bikontrol/.env` (mode `600`, owned by the deploy user) and, for local dev, in the gitignored `appsettings.Development.json`. `.env.example` only carries placeholders and is checked by Gitleaks on every PR.
- **Rotating `Jwt__Key`** immediately invalidates every issued access token (all users must log in again). Refresh tokens survive, so sessions renew on the next refresh; rotate during a low-traffic window.
- **Rotating DB credentials:** update `POSTGRES_PASSWORD` **and** the password inside `ConnectionStrings__DefaultConnection` together (they must match), then `./scripts/deploy.sh deploy`. Existing connections are re-established on restart.
- **Rotating SMTP / Google / Sentry:** update the matching vars, redeploy.
- After any rotation, verify: `./scripts/deploy.sh verify` (containers healthy) and a login + `/ready` check. Keep one previous value until the deploy is confirmed healthy, then discard it.
- **Secret scanning** (Gitleaks) runs in CI; Trivy scans the filesystem. Neither replaces rotating a leaked secret — if a secret ever reaches a commit, rotate it, don't just delete the line.
