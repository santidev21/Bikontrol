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
   - `./scripts/deploy.sh logs` (Serilog JSON lines; search for `"Level":"Error"`).
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
- The deploy already prunes to 7 backups; the weekly cron relies on it.

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

### Roll back the last deploy (code + config + DB from its pre-deploy backup)
```bash
cd /opt/bikontrol
./scripts/deploy.sh rollback
```
It brings the stack down, restores the DB dump taken at the start of that deploy,
restores the config tarball, and brings the stack back up. It then runs the smoke
test best-effort.

### Restore a specific backup (disaster recovery)
Backups live in `/opt/bikontrol/backups/backup-<timestamp>/db.sql.gz` (7-copy
retention; weekly cron). **Always verify a dump before trusting it.**

```bash
cd /opt/bikontrol

# 1. Pick the dump (newest first)
ls -dt backups/backup-*/db.sql.gz | head

# 2. Verify it restores into a THROWAWAY database (never touches the live one)
./scripts/db-verify-backup.sh backups/backup-XXXX/db.sql.gz

# 3. Stop the app, restore into the live database
docker compose down
docker compose up -d db && sleep 10
set -a && source .env && set +a
gunzip -c backups/backup-XXXX/db.sql.gz | \
  docker compose exec -T db psql -U "$POSTGRES_USER" -d "$POSTGRES_DB"

# 4. Bring everything back and confirm
docker compose up -d
./scripts/deploy.sh verify
npm run smoke https://bikontrol.santidev21.tech
```

> Migrations: production applies pending migrations on API startup. After
> restoring an **older** dump, the API will migrate the schema forward again on
> boot — expected. If the restored data is from before a destructive migration,
> review what that migration changed before starting the API.

### Integrity check after restore
Run the read-only audit; every block must return 0 rows:
```bash
psql ... -f scripts/db-integrity-audit.sql
```

## 5. Recovery objectives

| Objective | Target |
| --- | --- |
| **RPO** (max data loss) | ≤ 24h on the weekly cron; ≈ 0 for a failed deploy (pre-deploy dump) |
| **RTO** (time to restore) | ≤ 30 min from a verified backup |

Keep at least one dump **off the VPS** (object storage / another host) so a lost
host is recoverable — see the offsite note in DEPLOYMENT.md.

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
| Deploy | `./scripts/deploy.sh deploy` |
| Verify only | `./scripts/deploy.sh verify` |
| Roll back last deploy | `./scripts/deploy.sh rollback` |
| Manual DB backup | `./scripts/deploy.sh backup-db` |
| Verify a dump | `./scripts/db-verify-backup.sh <file.sql.gz>` |
| Smoke test | `npm run smoke https://bikontrol.santidev21.tech` |
| Readiness | `curl -fsS https://bikontrol.santidev21.tech/ready` |
| Metrics rules | `deploy/monitoring/alerts.yml` |
