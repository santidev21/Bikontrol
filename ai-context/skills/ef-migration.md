# EF Core migrations

From the repository root:

```bash
# Add a migration
npm run db:migration:add -- YourMigrationName

# Apply locally
npm run db:update
```

Rules:
- Never edit an applied migration — add a new one.
- Production DB updates currently require a manual migration run (see the To Do list).
