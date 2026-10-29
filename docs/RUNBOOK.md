# Bikontrol — Incident & Disaster Recovery Runbook

On-call playbook for the production deployment. It complements
[DEPLOYMENT.md](DEPLOYMENT.md) (how to deploy) with *what to do when something
breaks*. Keep it short, concrete and tested — an untested runbook is a guess.

> **Contacts / escalation** (fill in before going live):
> - Primary: _name — phone/Signal — email_
> - Backup: _name — phone/Signal — email_
> - Hosting provider support: _link / ticket URL_
> - DNS / domain registrar: _link_
> - Status page / user comms channel: _link_

## 1. First 5 minutes — triage

1. **Is it the app or the host?**
   - `./scripts/deploy.sh status` (container health) and
   - `curl -fsS https://bikontrol.santidev21.tech/ready` → `200` healthy, `503` DB down.
2. **What broke?** Look, in order:
   - `./scripts/deploy.sh logs` (Serilog compact JSON lines; errors have `"@l":"Error"`).
   - Sentry (if `Sentry__Dsn` is set) — newest issue, stack trace, `X-Request-Id`.
   - Prometheus/Grafana (`deploy/monitoring/`) — 5xx rate, latency, heap, `up`.
   - Host: `df -h`, `free -m`, `docker system df`.
3. **Stop the bleeding first**, fix root cause after (roll back, restore, scale, restart).
4. **Tell users** if it is user-visible (status page / post).

## 2. Common incidents

### API returns 5xx / is down
- Check `/ready`: if `503`, the DB is the problem (next section).
- If the process crashed: `docker compose ps`, then `docker compose logs api`.
- Config/secret change gone wrong → [roll back](#4-rollback--restore).

### Database unreachable (`/ready` = 503)
- `docker compose ps db` — is it healthy?
- `docker compose logs db` — disk full? corrupt volume?
- If the DB is lost/corrupt → [restore from backup](#4-rollback--restore).
- **Never** delete the `bikontrol_db_data` volume without a verified backup.

### Host disk full
- `df -h`, `docker system df`. Usual culprits: old images, logs, backups.
- `docker image prune -af`, `docker builder prune -af` (safe), trim `backups/`
  (keep the last few verified dumps), `journalctl --vacuum-time=3d`.
- The deploy already prunes to 14 backups (`BACKUP_RETENTION`); the daily cron relies on it.

### High error rate / latency (no full outage)
- Confirm with Grafana (5xx rate, p95) and Sentry.
- A single query or endpoint after a recent deploy → roll back.
- External dependency down (Sentry, SMTP, Google) — check provider status.

### Certificate / TLS expired
- `curl -vI https://bikontrol.santidev21.tech` and check the gateway's cert.
- Renew at the gateway (see the gateway repo), then re-run the smoke test.

## 3. Deploy failed

- The pipeline aborts and auto-rolls-back if the container health check or the
  [post-deploy smoke test](../scripts/smoke-test.sh) fails. Read `deploy.sh` output.
- Verify manually: `./scripts/deploy.sh verify` then `npm run smoke https://bikontrol.santidev21.tech`.
- If it left the stack down: `./scripts/deploy.sh rollback`.

## 4. Rollback / restore

### Roll back the last deploy (code + DB)
```bash
cd /opt/bikontrol
./scripts/deploy.sh rollback
```
`deploy` runs this automatically when the healthcheck or smoke test fails. It stops
`api`/`web`, restores the DB dump taken at the start of that deploy, and starts the
**previous image tag** (`PREVIOUS_IMAGE_TAG` in `/opt/bikontrol/.deploy-state`):
it checks out the commit behind that `sha-<commit>` tag, pulls the images from
GHCR and starts them without building. To go back to a specific release instead
(images only, no DB restore): `./scripts/deploy.sh rollback sha-<commit>`. Hosts
still on build-mode deploys fall back to the `:previous` local images, and then to
the config tarball with a rebuild. It then runs the smoke test best-effort and
exits non-zero if the DB restore failed.

### Restore a specific backup (disaster recovery)
Backups live in `/opt/bikontrol/backups/backup-<timestamp>/db.sql.gz` (14-copy
retention; daily cron at 03:00 UTC, plus one per deploy). When `BACKUP_OFFSITE_REMOTE`
is set, each dump is also uploaded **encrypted** (`backup-<ts>.sql.gz.enc`). Dumps are
taken with `--clean --if-exists` and restored with the `public` schema recreated, in a
single transaction: a restore either replaces the database entirely or leaves it
untouched.

```bash
cd /opt/bikontrol

# 1. Pick the dump (newest first)
ls -dt backups/backup-*/db.sql.gz | head

# 2. Restore: verifies the dump in a throwaway database first, stops api/web,
#    restores into the live database, starts everything again
./scripts/deploy.sh restore-db backups/backup-XXXX/db.sql.gz

# 3. Confirm
./scripts/deploy.sh verify
./scripts/deploy.sh smoke
```

From the offsite copy (host lost): fetch and decrypt, then restore as above.
```bash
rclone copyto "$BACKUP_OFFSITE_REMOTE/backup-XXXX.sql.gz.enc" db.sql.gz.enc
openssl enc -d -aes-256-cbc -pbkdf2 -in db.sql.gz.enc -out db.sql.gz -pass file:/opt/bikontrol/.backup-key
```
Keep a copy of `.backup-key` **outside** the VPS (password manager): without it the
offsite dumps cannot be decrypted.

### Restore drill
Restore a recent dump into a scratch environment (staging, or a throwaway Postgres
container with the same commands) at least once a quarter and after any change to the
backup scripts; record the date and the measured time below.

| Date | Environment | Result | Time |
| --- | --- | --- | --- |
| _fill in after the first drill on the VPS/staging_ | | | |

> Migrations: production applies migrations as a **dedicated deploy step**
> (`./scripts/deploy.sh migrate`, run automatically before `up`), not on API
> startup. After restoring an **older** dump, run `./scripts/deploy.sh migrate`
> (or a full `deploy`) to bring the schema forward. If the restored data is from
> before a destructive migration, review what that migration changed first.

### Integrity check after restore
Run the read-only audit; every block must return 0 rows:
```bash
psql ... -f scripts/db-integrity-audit.sql
```

## 5. Recovery objectives

| Objective | Target |
| --- | --- |
| **RPO** (max data loss) | ≤ 24h (daily cron); ≈ 0 for a failed deploy (pre-deploy dump) |
| **RTO** (time to restore) | ≤ 30 min from a verified backup |

Configure the offsite copy (`BACKUP_OFFSITE_REMOTE`, see DEPLOYMENT.md) so a lost
host is recoverable.

## 6. After the incident

1. Confirm the fix (health, `/ready`, smoke test, a real login + a write).
2. Write a short blameless post-incident note: **timeline, root cause, impact,
   what worked, action items** (turn items into issues).
3. Add/adjust an alert or a test so the same failure is caught earlier next time.
4. If a secret leaked, **rotate it** (never just delete the commit).

## 7. Quick reference

| Need | Command |
| --- | --- |
| Stack health | `./scripts/deploy.sh status` |
| Logs | `./scripts/deploy.sh logs` |
| Deploy | `./scripts/deploy.sh deploy sha-<commit>` (build mode: no tag) |
| Apply migrations | `./scripts/deploy.sh migrate` |
| Verify only | `./scripts/deploy.sh verify` |
| Roll back last deploy | `./scripts/deploy.sh rollback` |
| Manual DB backup | `./scripts/deploy.sh backup-db` |
| Verify a dump | `./scripts/db-verify-backup.sh <file.sql.gz>` |
| Restore a dump | `./scripts/deploy.sh restore-db <file.sql.gz>` |
| Smoke test | `npm run smoke https://bikontrol.santidev21.tech` |
| Readiness | `curl -fsS https://bikontrol.santidev21.tech/ready` |
| Metrics rules | `deploy/monitoring/alerts.yml` |
