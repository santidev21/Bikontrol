# Bikontrol

Bikontrol is a full-stack web app for motorcycle owners to track motorcycles, km history, and maintenance plans.

## Tech stack
- Frontend: Angular 18, SCSS, Tailwind CSS, Jest
- Backend: .NET 8, Clean Architecture, EF Core, xUnit
- Database: PostgreSQL
- Authentication: JWT

## Repository layout
```text
C:\Dev\Bikontrol
├─ Bikontrol/        # .NET solution
├─ bikontrol-web/    # Angular application
├─ ai-context/       # project context and working specs
└─ package.json      # root orchestration scripts
```

## Quick start
From the repository root:

```bash
npm run bikontrol
```

That command starts both apps together:
- Frontend at `http://localhost:4201`
- API at `https://localhost:7179`

If you want only one side:
- `npm run bikontrol-ui`
- `npm run bikontrol-api`

## Root scripts

| Command | Purpose |
| --- | --- |
| `npm run bikontrol` | Starts frontend and backend together |
| `npm run bikontrol-ui` | Starts only the Angular app |
| `npm run bikontrol-api` | Starts only the API |
| `npm run build` | Builds frontend and backend |
| `npm run test` | Runs frontend and backend tests |

## Useful backend paths
- API project: `Bikontrol/Bikontrol.API`
- Persistence project: `Bikontrol/Bikontrol.Persistence`
- Backend solution: `Bikontrol/Bikontrol.sln`

## Notes
- The root `package.json` is an orchestration layer; the frontend keeps its own Angular scripts inside `bikontrol-web/package.json`.
- Run `npm run bikontrol-ui` or `npm run bikontrol-api` when you want to work on only one side.
- Use `npm run db:update` after creating or receiving new EF migrations.
- Add a migration with `npm run db:migration:add -- YourMigrationName`.
- The API runner skips `launchSettings.json` and forces `https://localhost:7179` so it does not collide with the default HTTP port.
- The frontend runner forces `http://localhost:4201` so it does not collide with the default Angular port.
