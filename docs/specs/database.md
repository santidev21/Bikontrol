# Database

- PostgreSQL via EF Core (`AppDbContext`, repositories, entity configuration in `Bikontrol.Persistence`).
- Users own motorcycles; motorcycles store km history.
- Maintenance has default types and user-defined maintenance items.
- Soft deletes for several entities through `IsEnabled`.
- Local migrations: `npm run db:migration:add -- YourMigrationName`, then `npm run db:migrate`. See [db-migrations](../../.opencode/skills/db-migrations/SKILL.md).
- Production applies pending migrations automatically at API startup (`db.Database.Migrate()` in non-Development); no manual step needed on deploy.
- Local Docker maps Postgres to `127.0.0.1:5434`; in production it lives on `bikontrol-internal-net` only.

## Data integrity guards

- Multi-step writes are atomic via `ITransactionManager` (`Bikontrol.Persistence.TransactionManager`): maintenance record + odometer advance, motorcycle + initial km, km rollback.
- Optimistic concurrency with the Postgres `xmin` system column on `Motorcycles`, `UserMaintenanceTypes`, `users` (Npgsql `UseXminAsConcurrencyToken`; the scaffolded `AddColumn xmin` is ignored by the Npgsql SQL generator — no physical column is created). Conflicts surface as `DbUpdateConcurrencyException` → HTTP 409.
- CHECK constraints: `CK_MotorcycleKmHistories_Km_NonNegative`, `CK_MotorcycleMaintenanceRecords_PerformedKm_NonNegative`, `CK_UserMaintenanceTypes_PositiveInterval` (interval > 0 for the selected `TrackingType`).
- Service validation mirrors the constraints (positive interval per tracking type; historical maintenance records below the odometer don't move it and never fail the operation).
- Read-only audit: `scripts/db-integrity-audit.sql` — every block must return 0 rows. Run it before any deploy/migration once real users exist.
- Backups: `npm run db:backup` / `db:restore` locally; `deploy.sh backup-db` on the VPS. Always back up before deploying migrations.
