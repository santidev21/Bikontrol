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
cd /opt/bikontrol && ./scripts/deploy.sh deploy sha-<commit>   # published images
cd /opt/bikontrol && ./scripts/deploy.sh deploy                # build on this host
```

**Build once, deploy by tag.** On `main`, CI (`publish-images`) runs after the tests, Gitleaks, Trivy fs and compose validation, builds the API and web images once, scans both with Trivy (HIGH/CRITICAL fail the job) and pushes them to GHCR as `ghcr.io/<owner>/bikontrol-{api,web}:sha-<commit>`. The `deploy` job (GitHub environment `production`, which carries the manual approval — configure **Required reviewers** in Settings → Environments) then runs `deploy.sh deploy sha-<commit>` over SSH: the host checks out that exact commit (compose file and scripts match the images), pulls the images and never builds. CodeQL, SonarCloud and E2E run as required status checks on the PR, so only commits that passed them reach `main`. The web bundle bakes the public Google client id from the repository variable `GOOGLE_CLIENT_ID`.

Each deploy runs, in order: backup + verify → integrity audit → checkout/pull → pull images (or build) → **apply migrations (dedicated step)** → up → **container healthcheck** → **post-deploy smoke test**. The serving tag and the previous one are recorded in `.deploy-state`. If the healthcheck **or** the smoke test fails, the deploy **rolls back automatically** (restores the pre-deploy DB dump and starts the previous tag; hosts still on build-mode deploys use the `:previous` local images, then a rebuild from the config backup). Roll back manually with `./scripts/deploy.sh rollback [sha-<commit>]`.

The package visibility of the GHCR images is private by default; the deploy job logs the host in with the run's short-lived `GITHUB_TOKEN` (`packages: read`). For manual tagged deploys, run `docker login ghcr.io` once on the host or export `REGISTRY_USER`/`REGISTRY_TOKEN`.

**Migrations as a dedicated step.** Production does **not** migrate on API startup (`Database__RunMigrationsOnStartup=false`). Instead, `deploy.sh deploy` runs `deploy.sh migrate`, which executes the freshly built API image in migration-only mode (`docker compose run --rm --no-deps api --migrate`), on the internal network, before any container is started for traffic. It is idempotent and fails the deploy (with the pre-deploy backup already taken) before anything is started. Migrate manually with `./scripts/deploy.sh migrate`. `Database__RunMigrationsOnStartup` now defaults to `false` in `docker-compose.yml`; `docker-compose.local.yml` and the E2E/ZAP workflows set it to `true` because they do not run `deploy.sh`.

**Worker for the daily jobs.** The reminder and retention jobs run in a separate `worker` service (same API image, `Jobs__Enabled` turned on, internal network only, no fixed container name). The `api` service turns `Jobs__Enabled` off, so the API can be replicated without running the jobs once per replica. The jobs keep their Postgres advisory locks, so an accidental overlap is harmless. `deploy.sh` waits for the worker's healthcheck as well. Running everything in one process (leave `Jobs__Enabled` unset) is still the default outside compose.

The smoke test (`scripts/smoke-test.sh`, also `npm run smoke <url>`) hits the public origin and asserts `/health`, `/ready` (DB), the SPA shell and the auth endpoints — read-only, no credentials.

### Staging

The same compose files and script serve a staging environment; point it at a separate deploy dir and host:

```bash
# On the staging VPS / directory:
DEPLOY_DIR=/opt/bikontrol-staging \
SMOKE_URL=https://staging.bikontrol.santidev21.tech \
  ./scripts/deploy.sh deploy
```

Staging deploys in build mode (no tag) because the web bundle bakes the API URL and the Google client id, so one image cannot be promoted across domains until the frontend reads its config at runtime. `DEPLOY_DIR`, `SMOKE_URL`, `LOG_FILE` and `BACKUP_RETENTION` are all environment-overridable, so staging shares the code but keeps its own data, backups and domain. Add a matching gateway site config (see the gateway repo) and a `.env` with staging values.

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

Every `deploy` call already creates a compressed DB dump (`pg_dump` → `backups/backup-<timestamp>/db.sql.gz`) before pulling new code. Backups are kept in persistent `backups/` (not `/tmp`) with 14-copy retention (`BACKUP_RETENTION`). Dumps use `--clean --if-exists` so they restore over a live database.

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

**Automatic daily backups** (VPS cron — 03:00 UTC):

```bash
# One-time: install the daily cron (idempotent, replaces any previous bikontrol backup cron)
sudo /opt/bikontrol/scripts/deploy.sh install-cron
# Custom schedule: sudo /opt/bikontrol/scripts/deploy.sh install-cron "0 4 * * *"
# Remove: sudo /opt/bikontrol/scripts/deploy.sh remove-cron
# Check: crontab -l | grep bikontrol
```

Manual cron line (equivalent):

```cron
0 3 * * * /opt/bikontrol/scripts/deploy.sh backup-db >> /var/log/bikontrol-backup.log 2>&1
```

**Offsite, encrypted copy.** Set these in the deploy `.env`; every backup (cron and deploy) is then encrypted with AES-256 and uploaded with `rclone`. A failed upload is logged as a warning and never aborts a deploy.

```bash
BACKUP_OFFSITE_REMOTE=b2:bikontrol-backups        # any configured rclone remote path
BACKUP_ENCRYPTION_KEY_FILE=/opt/bikontrol/.backup-key   # chmod 600; also keep a copy outside the VPS
```

Create the key once with `openssl rand -base64 48 > /opt/bikontrol/.backup-key && chmod 600 /opt/bikontrol/.backup-key` and configure the remote with `rclone config`.

Recovery: `./scripts/deploy.sh restore-db backups/backup-xxx/db.sql.gz` (verifies the dump, restores in a single transaction, restarts the app). `./scripts/deploy.sh rollback` uses the same restore and also returns to the previous images. See [RUNBOOK](RUNBOOK.md) §4.

## Email deliverability

Bikontrol sends through plain SMTP, so any transactional provider works (Resend, Postmark, Amazon SES):
set `Smtp__Host`, `Smtp__Port` (587), `Smtp__Username`, `Smtp__Password` and `Smtp__FromEmail` in `.env`
and redeploy. Typical Resend values: host `smtp.resend.com`, username `resend`, password = the API key.

Gmail and Yahoo reject or spam-folder bulk mail without these DNS records on the **From domain**
(`Smtp__FromEmail`):

1. **SPF** — one `v=spf1 include:<provider> ~all` TXT record on the domain (never two).
2. **DKIM** — the CNAME/TXT records your provider shows when you verify the domain
   (`<selector>._domainkey`).
3. **DMARC** — `_dmarc.<domain>` TXT, e.g. `v=DMARC1; p=none; rua=mailto:you@domain` at first, moving to
   `quarantine`/`reject` once the reports look clean.

Check them with `./scripts/check-email-dns.sh <domain> <dkim-selector>` (read-only `dig` lookups).
The reminder digest also sends `List-Unsubscribe` / `List-Unsubscribe-Post` headers, which Gmail needs
to show its own unsubscribe button. After changing provider, send a test through the forgot-password
flow and check the message headers show `spf=pass dkim=pass dmarc=pass`.

## Observability

- **Structured logs (Serilog)** go to stdout and to the rolling file in `/app/logs` as one compact JSON object per line in production (`@t` timestamp, `@m` rendered message, `@l` level — absent for Information —, `@x` exception, plus the trace/span ids and the structured properties); Development keeps readable text. Filter errors with `jq 'select(.["@l"]=="Error")'`. Each request is logged once (`HTTP {Method} {Path} responded {StatusCode} in {Elapsed} ms`).
- **Request correlation:** errors echo an `X-Request-Id` header; the same id appears in the log line, so a user report maps to the exact log entry.
- **Error tracking (Sentry):** set `Sentry__Dsn` to capture unhandled exceptions with environment, release and trace context. It is a no-op when the DSN is empty (local dev/CI). PII is disabled (`SendDefaultPii=false`).
- **Uptime monitoring:** point an external monitor (UptimeRobot/Better Stack/`healthchecks.io`, or the gateway) at `GET /ready`; it returns `503` when PostgreSQL is unreachable, so an alert fires before users notice.
- **Metrics (OpenTelemetry → Prometheus):** set `Metrics__Enabled=true` (default in `docker-compose.yml`) and the API exposes `/metrics` in Prometheus format (request rate/latency/status, HttpClient, .NET runtime). Scrape config, alert rules and a Grafana dashboard live in [`deploy/monitoring/`](../deploy/monitoring/README.md). The endpoint is internal-only — the public gateway never proxies it. Both the API and the worker expose it; the daily jobs publish `bikontrol_job_runs_total{task,result}`, `bikontrol_job_duration_seconds`, `bikontrol_job_last_success_timestamp_seconds{task}`, `bikontrol_reminders_total{stage}` and `bikontrol_retention_deleted_total{kind}`.
- **Monitoring stack:** `docker compose -f docker-compose.monitoring.yml up -d` starts Prometheus, Alertmanager (email through the app's SMTP account to `ALERT_EMAIL_TO`) and Grafana (provisioned datasource and dashboard), all bound to loopback and pinned by version and digest. Set `ALERT_EMAIL_TO` and `GRAFANA_ADMIN_PASSWORD` in `.env`. Send a test alert with `curl -XPOST localhost:9093/api/v2/alerts -H 'Content-Type: application/json' -d '[{"labels":{"alertname":"TestAlert","severity":"warning"}}]'`.
- **Tracing:** set `Tracing__OtlpEndpoint` to an OTLP collector and the API/worker export traces for ASP.NET Core, outgoing HTTP and Npgsql. Empty = disabled.

For incident handling and disaster recovery, see [RUNBOOK.md](RUNBOOK.md).

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
| `Metrics__Enabled` | Expose OpenTelemetry metrics at `/metrics` (Prometheus format). Default `true` in compose; internal-network only. See [`deploy/monitoring/`](../deploy/monitoring/README.md). |
| `Reminders__Enabled` / `Reminders__EmailEnabled` / `Reminders__PushEnabled` / `Reminders__DailyHourUtc` / `Reminders__DedupeDays` | Maintenance reminder engine (defaults: `true` / `true` / `true` / `8` (UTC) / `3`). Email digest needs SMTP; push needs VAPID. |
| `WebPush__PublicKey` / `WebPush__PrivateKey` / `WebPush__Subject` | Web Push (VAPID). Generate with `npx web-push generate-vapid-keys --json`. Empty keys disable push (email still works). **Rotating the keys invalidates all existing subscriptions** (users must re-enable notifications). |
| `DemoUser__Email` / `DemoUser__FullName` | Demo user identity (defaults `demo@bikontrol.com` / `Usuario Demo`); only used when `Demo__Enabled=true` |
| `Frontend__BaseUrl` | Base URL for password-reset links (default `https://bikontrol.santidev21.tech`) |
| `Smtp__Host` / `Smtp__Port` / `Smtp__Username` / `Smtp__Password` / `Smtp__FromEmail` (+ `Smtp__FromName`, `Smtp__EnableSsl`) | SMTP for recovery emails (**required** in prod, otherwise reset links are only logged) |
| `Cors__AllowedOrigins` | Comma-separated browser origins |

### Secret management & rotation

- **Never in the repo.** Real values live in `/opt/bikontrol/.env` (mode `600`, owned by the deploy user) and, for local dev, in the gitignored `appsettings.Development.json`. `.env.example` only carries placeholders and is checked by Gitleaks on every PR.
- **Rotating `Jwt__Key`** immediately invalidates every issued access token (all users must log in again). Refresh tokens survive, so sessions renew on the next refresh; rotate during a low-traffic window.
- **Rotating DB credentials:** update `POSTGRES_PASSWORD` **and** the password inside `ConnectionStrings__DefaultConnection` together (they must match), then redeploy the current tag (`./scripts/deploy.sh deploy $(. .deploy-state; echo $IMAGE_TAG)`). Existing connections are re-established on restart.
- **Rotating SMTP / Google / Sentry:** update the matching vars, redeploy.
- After any rotation, verify: `./scripts/deploy.sh verify` (containers healthy) and a login + `/ready` check. Keep one previous value until the deploy is confirmed healthy, then discard it.
- **Secret scanning** (Gitleaks) runs in CI; Trivy scans the filesystem. Neither replaces rotating a leaked secret — if a secret ever reaches a commit, rotate it, don't just delete the line.
