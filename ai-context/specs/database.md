# Database

- PostgreSQL via EF Core (`AppDbContext`, repositories, entity configuration in `Bikontrol.Persistence`).
- Users own motorcycles; motorcycles store km history.
- Maintenance has default types and user-defined maintenance items.
- Soft deletes for several entities through `IsEnabled`.
- Local migrations: `npm run db:migration:add -- YourMigrationName`, then `npm run db:update`. See [skills/ef-migration.md](../skills/ef-migration.md).
