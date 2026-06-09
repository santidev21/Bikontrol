# Bikontrol

Bikontrol is a full-stack web app for motorcycle owners to track motorcycles, km history, and maintenance plans.

## Tech stack
- Frontend: Angular 18, SCSS, Tailwind CSS, Jest
- Backend: .NET 8, Clean Architecture, EF Core, xUnit
- Database: PostgreSQL
- Authentication: JWT

## Repository layout
```text
/home/santidev21/Dev/Bikontrol
├─ Bikontrol/        # .NET solution and backend tests
├─ bikontrol-web/    # Angular application
├─ ai-context/       # project context, playbooks, and AI notes
├─ scripts/          # orchestration scripts
└─ package.json      # root scripts for local dev and CI parity
```

## Source of truth for AI work
- [ai-context/agent.md](/home/santidev21/Dev/Bikontrol/ai-context/agent.md) is the canonical project context.
- `ai-context/helpers/` contains smaller repo-specific playbooks when they are needed.
- If you add more specialized instructions later, keep them close to the context that uses them.

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

## Backend setup
The API loads `Bikontrol/Bikontrol.API/appsettings.Development.json` when `ASPNETCORE_ENVIRONMENT=Development`.

Before running the API for the first time on a new clone, generate a JWT secret and put it in that file:

```bash
openssl rand -base64 48
```

Put the generated value in `Jwt:Key` inside `Bikontrol/Bikontrol.API/appsettings.Development.json`. Keep `appsettings.json` for shared defaults only.

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
- The root frontend runner forces `http://localhost:4201` so it does not collide with the default Angular port.
- If you run Angular directly from `bikontrol-web/` with `npm start`, it still uses the default `4200` unless you pass a different port.

## Continuous Integration
- A GitHub Actions workflow runs on every push and pull request to `main`.
- It executes backend tests with `dotnet test` and frontend tests with `npm test`.
- The frontend job uses `npm install` so it stays resilient while the lockfile is being aligned.
- The workflow lives in `.github/workflows/ci.yml`.
