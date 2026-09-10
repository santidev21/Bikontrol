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
npm run db:update
```

Raw form:

```bash
dotnet ef migrations add YourMigrationName --project Bikontrol/Bikontrol.Persistence/Bikontrol.Persistence.csproj --startup-project Bikontrol/Bikontrol.API/Bikontrol.API.csproj
dotnet ef database update --project Bikontrol/Bikontrol.Persistence/Bikontrol.Persistence.csproj --startup-project Bikontrol/Bikontrol.API/Bikontrol.API.csproj
```

Rules:
- Never edit an applied migration — add a new one.
- Run `npm run db:update` after creating or receiving new migrations.
- Production DB updates currently require a manual migration run (see the To Do list).
