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
- CHECK constraints: `CK_MotorcycleKmHistories_Km_NonNegative`, `CK_MotorcycleMaintenanceRecords_PerformedKm_NonNegative`, `CK_MotorcycleMaintenanceRecords_Cost_NonNegative` (`Cost` is nullable `numeric(12,2)`), `CK_UserMaintenanceTypes_PositiveInterval` (interval > 0 for the selected `TrackingType`).
- Service validation mirrors the constraints (positive interval per tracking type; historical maintenance records below the odometer don't move it and never fail the operation).
- Read-only audit: `scripts/db-integrity-audit.sql` — every block must return 0 rows. Run it before any deploy/migration once real users exist.
- Backups: `npm run db:backup` / `db:restore` locally; `deploy.sh backup-db` on the VPS. Always back up before deploying migrations.

## Audit log

- `audit_logs` (append-only) records who changed what: `UserId` (null for background jobs), `EntityName`, `EntityId`, `Action` (`Created`/`Updated`/`Deleted`), `Changes` (jsonb diff, sensitive columns redacted) and `CreatedAt`.
- Written automatically by `AppDbContext.SaveChangesAsync` for user-facing entities (`User`, `Motorcycle`, `UserMaintenance`, `MotorcycleMaintenanceRecord`, `MotorcycleKmHistory`, `MaintenanceRecordAttachment`, `FuelUp`); infrastructure tables (tokens, push subscriptions, reminder logs) are intentionally excluded.
- Read through `GET /api/users/me/activity`.
- Attachments: `maintenance_record_attachments` stores resized image data URLs (JPEG/PNG/WebP, ≤1 MB decoded), cascade-deleted with their maintenance record.

## Fuel-ups and budgets

- `FuelUps` stores one row per refuelling (`MotorcycleId`, `FueledAt`, `OdometerKm`, `Litres numeric(8,3)`, `TotalCost numeric(12,2)`, `IsFullTank`, `Station`, `Notes`); `PricePerLitre` is derived in code and never stored. CHECK constraints: `CK_FuelUps_OdometerKm_NonNegative`, `CK_FuelUps_Litres_Positive`, `CK_FuelUps_TotalCost_NonNegative`. Indexes on `(MotorcycleId, FueledAt)` and `(MotorcycleId, OdometerKm)`.
- A fuel-up advances the motorcycle km history in the same transaction when its odometer is above the current reading (a backdated entry below the odometer does not move it and never fails).
- `users.MonthlyMaintenanceBudget` / `users.AnnualMaintenanceBudget` (nullable `numeric(12,2)`) hold the optional maintenance budgets; `null` disables a budget. They are reset on account anonymization and returned by `GET /api/users/me`.
