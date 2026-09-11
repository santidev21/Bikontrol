---
name: db-migrations
description: Add and apply EF Core migrations for Bikontrol. Use when creating or applying PostgreSQL migrations in Bikontrol.Persistence.
---

# EF Core migrations

Migrations live in `Bikontrol/Bikontrol.Persistence`. Prefer the root scripts, from the repo root:

```bash
# Add a migration
npm run db:migration:add -- YourMigrationName

# Apply pending migrations
npm run db:migrate
```

Raw form:

```bash
dotnet ef migrations add YourMigrationName --project Bikontrol/Bikontrol.Persistence/Bikontrol.Persistence.csproj --startup-project Bikontrol/Bikontrol.API/Bikontrol.API.csproj
dotnet ef database update --project Bikontrol/Bikontrol.Persistence/Bikontrol.Persistence.csproj --startup-project Bikontrol/Bikontrol.API/Bikontrol.API.csproj
```

Rules:
- Never edit an applied migration — add a new one.
- Run `npm run db:migrate` after creating or receiving new migrations.
- Production applies pending migrations automatically: the API runs `db.Database.Migrate()` on startup in any non-Development environment (`Bikontrol.API/Program.cs`), so no manual migration step is needed on deploy. Local dev applies them manually via `npm run db:migrate`.
