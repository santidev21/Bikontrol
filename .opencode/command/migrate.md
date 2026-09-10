---
description: Add or apply an EF Core migration for Bikontrol (migrations live in Persistence).
---

Follow the `db-migrations` skill. Prefer the root scripts, from the repo root:

```bash
npm run db:migration:add -- YourMigrationName
npm run db:update
```

Rules: never edit an applied migration — add a new one. Migrations live in `Bikontrol/Bikontrol.Persistence`.
