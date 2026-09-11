# Database

- PostgreSQL via EF Core (`AppDbContext`, repositories, entity configuration in `Bikontrol.Persistence`).
- Users own motorcycles; motorcycles store km history.
- Maintenance has default types and user-defined maintenance items.
- Soft deletes for several entities through `IsEnabled`.
- Local migrations: `npm run db:migration:add -- YourMigrationName`, then `npm run db:migrate`. See [db-migrations](../../.opencode/skills/db-migrations/SKILL.md).
- Production applies pending migrations automatically at API startup (`db.Database.Migrate()` in non-Development); no manual step needed on deploy.
- Local Docker maps Postgres to `127.0.0.1:5434`; in production it lives on `bikontrol-internal-net` only.
