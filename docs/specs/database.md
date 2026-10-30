# Database

- PostgreSQL via EF Core (`AppDbContext`, repositories, entity configuration in `Bikontrol.Persistence`).
- Users own motorcycles; motorcycles store km history.
- Maintenance has default types and user-defined maintenance items.
- Soft deletes for several entities through `IsEnabled`.
- Local migrations: `npm run db:migration:add -- YourMigrationName`, then `npm run db:migrate`. See [db-migrations](../../.opencode/skills/db-migrations/SKILL.md).
- Production applies pending migrations automatically at API startup (`await db.Database.MigrateAsync()` in non-Development); no manual step needed on deploy.
- Local Docker maps Postgres to `127.0.0.1:5434`; in production it lives on `bikontrol-internal-net` only.

## Data integrity guards

- Multi-step writes are atomic via `ITransactionManager` (`Bikontrol.Persistence.TransactionManager`): maintenance record + odometer advance, motorcycle + initial km, km rollback.
- Optimistic concurrency with the Postgres `xmin` system column on `Motorcycles`, `UserMaintenanceTypes`, `users`, `FuelUps`, `MotorcycleKmHistories`, `MotorcycleMaintenanceRecords` (Npgsql 9 removed `UseXminAsConcurrencyToken`, so the shadow `xmin` property is mapped explicitly to the system column — no physical column is created). Conflicts surface as `DbUpdateConcurrencyException` → HTTP 409. Odometer writes (`KmHistoryService.AddKmAsync`) also take a `SELECT … FOR UPDATE` row lock on the motorcycle inside a transaction, so concurrent writers for one bike are serialised instead of racing on a stale reading.
- CHECK constraints: `CK_MotorcycleKmHistories_Km_NonNegative`, `CK_MotorcycleMaintenanceRecords_PerformedKm_NonNegative`, `CK_MotorcycleMaintenanceRecords_Cost_NonNegative` (`Cost` is nullable `numeric(12,2)`), `CK_UserMaintenanceTypes_PositiveInterval` (interval > 0 for the selected `TrackingType`).
- Service validation mirrors the constraints (positive interval per tracking type; historical maintenance records below the odometer don't move it and never fail the operation).
- Read-only audit: `scripts/db-integrity-audit.sql` — every block must return 0 rows. Run it before any deploy/migration once real users exist.
- Backups: `npm run db:backup` / `db:restore` locally; `deploy.sh backup-db` on the VPS. Always back up before deploying migrations.

## Audit log

- `audit_logs` (append-only) records who changed what: `UserId` (null for background jobs), `EntityName`, `EntityId`, `Action` (`Created`/`Updated`/`Deleted`), `Changes` (jsonb diff; sensitive columns redacted and inline images/attachments — `Image`, `DataUrl`, any `data:` value — replaced by a `[binary omitted: N chars]` marker) and `CreatedAt`.
- Written automatically by `AppDbContext.SaveChangesAsync` for user-facing entities (`User`, `Motorcycle`, `UserMaintenance`, `MotorcycleMaintenanceRecord`, `MotorcycleKmHistory`, `MaintenanceRecordAttachment`, `FuelUp`); infrastructure tables (tokens, push subscriptions, reminder logs) are intentionally excluded.
- Read through `GET /api/users/me/activity`.
- Attachments: `maintenance_record_attachments` holds the image either as a resized data URL in `DataUrl` (legacy rows, or `Storage:Enabled=false`) or, when object storage is on, as `StorageKey` pointing into the bucket with `DataUrl` empty; `SizeBytes` feeds the per-user quota. Cascade-deleted with their maintenance record (the objects are removed by the service, see [ADR 005](../adr/005-object-storage.md)).

## Query shape

- User emails are stored normalized (trim + lower); lookups compare against the normalized form so the unique index serves them (no `ToLower()`). Migration `NormalizeUserEmails` rewrites legacy rows; audit check 17 reports any that could not be rewritten because the normalized address already belongs to another account.
- Per-motorcycle "latest km", "initial date" and per-maintenance "last record" are computed in SQL (`GROUP BY` / anti-join) instead of loading every row and grouping in memory.
- `Motorcycle.Image` is `default.png` or, while the photo is still in the database (legacy rows, or `Storage:Enabled=false`), a data URL of up to ~1 MB; with object storage the photo is `ImageKey` + `ImageVersion` + `ImageSizeBytes` and `Image` stays `default.png`. API responses always expose a signed media URL instead of base64. Only the endpoints that return the entity load it. Callers that need labels or ids (`MotorcycleOwnership`, due reminders) use `GetNamesByUserIdAsync`, a narrow projection.

## Data retention

- `RetentionBackgroundService` (a second daily background job at `Retention:DailyHourUtc`, default 04:00 UTC — off-peak, clear of the reminder engine's 08:00) runs `RetentionService.PurgeAsync` once a day.
- Bulk deletes through EF `ExecuteDeleteAsync` — no change tracker, no audit rows (same rationale as `AccountRepository`):
  - `refresh_tokens`: rows that expired **or** were revoked more than `Retention:RefreshTokenDays` days ago (default 30). Rotation mints a row per session refresh, so the table would grow without bound otherwise.
  - `reminder_logs`: rows older than `Retention:ReminderLogDays` days (default 90; the dedupe window needs 3).
  - `audit_logs`: **never** purged by default (`Retention:AuditLogDays = 0`); set a positive value to opt into an audit retention policy.
- Configure via `Retention__*` in `.env`/compose; `Retention:Enabled=false` stops the scheduler. A failed run is logged and retried on the next tick — the scheduler never dies.

## Fuel-ups and budgets

- `FuelUps` stores one row per refuelling (`MotorcycleId`, `FueledAt`, `OdometerKm`, `Litres numeric(8,3)`, `TotalCost numeric(12,2)`, `IsFullTank`, `Station`, `Notes`); `PricePerLitre` is derived in code and never stored. CHECK constraints: `CK_FuelUps_OdometerKm_NonNegative`, `CK_FuelUps_Litres_Positive`, `CK_FuelUps_TotalCost_NonNegative`. Indexes on `(MotorcycleId, FueledAt)` and `(MotorcycleId, OdometerKm)`.
- A fuel-up advances the motorcycle km history in the same transaction when its odometer is above the current reading (a backdated entry below the odometer does not move it and never fails).
- `users.MonthlyMaintenanceBudget` / `users.AnnualMaintenanceBudget` (nullable `numeric(12,2)`) hold the optional maintenance budgets; `null` disables a budget. They are reset on account anonymization and returned by `GET /api/users/me`.
