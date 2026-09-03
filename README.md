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

### Known dependency notes
- `AutoMapper` is pinned to `12.0.1`. Versions `>= 15` require a paid license and pull .NET 9/10 + `Microsoft.IdentityModel` 8.x dependencies that conflict with the net8.0 JWT stack. The upstream advisory `GHSA-rvv3-g6hj-g44x` (DoS via deep recursive object graphs) does not apply here: Bikontrol only maps flat, fixed-shape DTOs with no recursive/self-referencing graphs reachable from user input. The advisory is suppressed in `Bikontrol/Directory.Build.props` with that rationale.

## Production deployment

Bikontrol is served at `https://bikontrol.santidev21.tech/` behind the `vps-gateway` reverse proxy.

Architecture (per `vps-gateway/docs/STANDARD.md`):

```
Internet → gateway (nginx) → bikontrol (Angular, :80)
                          → bikontrol-api (.NET, :8080) → bikontrol-db (PostgreSQL)
```

- `bikontrol-net` (external, shared with the gateway): `bikontrol` + `bikontrol-api`.
- `bikontrol-internal-net` (internal): database only. The DB is **never** on the shared network.

One-time VPS setup:

```bash
git clone <repo> /opt/bikontrol
cd /opt/bikontrol
cp .env.example .env   # fill in real values, no CHANGE_ME left
# create the shared networks (or let the gateway's init-networks.sh do it)
docker network create bikontrol-net
docker network create bikontrol-internal-net
```

Deploy (automatic on push to `main` via GitHub Actions, or manual):

```bash
cd /opt/bikontrol && ./scripts/deploy.sh deploy
```

### Gateway integration

1. Create DNS A record `bikontrol.santidev21.tech` → VPS IP.
2. Issue the certificate (see `vps-gateway/AGENTS.md`).
3. Copy `deploy/bikontrol.santidev21.tech.conf` into the gateway's `sites-enabled/` and add `bikontrol-net` to the gateway `docker-compose.yml` networks and `init-networks.sh`.
4. Reload: `docker exec gateway nginx -t && docker exec gateway nginx -s reload`.

### Required secrets (`.env`, never committed)

| Variable | Purpose |
|---|---|
| `POSTGRES_DB` / `POSTGRES_USER` / `POSTGRES_PASSWORD` | Database credentials |
| `ConnectionStrings__DefaultConnection` | Full Npgsql connection string |
| `Jwt__Key` | JWT signing key (generate with `openssl rand -base64 48`) |
| `Jwt__Issuer` / `Jwt__Audience` | JWT issuer/audience (defaults provided) |
| `Cors__AllowedOrigins` | Comma-separated browser origins |

## Continuous Integration
- A GitHub Actions workflow runs on every push and pull request to `main`.
- It executes backend tests with `dotnet test` and frontend tests with `npm test`.
- It validates the compose files and deploys to the VPS on push to `main` (requires `VPS_SSH_PRIVATE_KEY`, `VPS_HOST`, `VPS_USER` secrets).
- The frontend job uses `npm install` so it stays resilient while the lockfile is being aligned.
- The workflow lives in `.github/workflows/ci.yml`.
