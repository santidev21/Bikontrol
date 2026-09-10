# Bikontrol Project Context

This file is the working context for Bikontrol. Keep it updated when architecture, routing, scripts, or conventions change. See [docs/specs/](docs/specs/) for detail docs.

## Project Snapshot
Motorcycle tracking and maintenance app:
- Angular 18 frontend (SCSS, Tailwind CSS, PWA service worker; Jest tests)
- .NET 8 backend with Clean Architecture (API, Application, Domain, Infrastructure, Persistence, Shared)
- PostgreSQL 16 via EF Core (migrations in Persistence, applied via root `db:update` script)
- JWT authentication (login/register), per-user salt password hashing, soft deletes
- Root `package.json` orchestrates both sides (`bikontrol`, `build`, `test`, `db:*` scripts)

## Repository Layout
```text
Bikontrol/
├─ Bikontrol/       # .NET solution (Bikontrol.sln: API, Application, Domain, Infrastructure, Persistence, Shared, Tests)
├─ bikontrol-web/   # Angular 18 application (src/app)
├─ scripts/         # Orchestration scripts (run-bikontrol.mjs)
├─ deploy/          # Deployment configs
├─ docker/          # Dockerfiles (api, web)
├─ docs/            # Guides, specs (docs/specs/)
├─ .opencode/       # AI home: agent/, command/, skills/ (tracked; local plugin scaffold ignored)
├─ .github/         # CI/CD workflows
├─ docker-compose.yml
├─ docker-compose.local.yml
├─ package.json     # Root orchestration scripts
├─ opencode.json    # opencode config: instructions, MCP, permissions
└─ .env.example
```

## Backend Architecture
Clean Architecture layers: `API` (controllers) → `Application` (services, DTOs; AutoMapper 12 pinned) → `Domain` (entities with soft deletes) → `Persistence` (EF Core, `DbContext`, migrations) + `Infrastructure` → `Shared`. API loads `Bikontrol.API/appsettings.Development.json` in Development; JWT secret generated per clone (`openssl rand -base64 48` into `Jwt:Key`).

## Frontend Architecture
Angular 18 SPA in `bikontrol-web/src/app` (Tailwind + SCSS, PWA via `ngsw-config.json`, SweetAlert2 dialogs). Tests are Jest (`npm test` → `jest --passWithNoTests --runInBand`).

## Commands (run from repo root via root scripts unless noted)
- Both: `npm run build` · `npm run test` (see `/test`)
- Backend: `npm run test:api` (= `dotnet test Bikontrol/Bikontrol.sln`) · `npm run build:api` (see `backend-test` skill)
- Frontend: `npm run test:ui` · `npm run build:ui` (see `frontend-test`)
- Migrations: `npm run db:migration:add -- <Name>` · `npm run db:update` (see `db-migrations`, or `/migrate`)
- Single side: `npm run bikontrol-ui` · `npm run bikontrol-api`
- Docker: `docker compose -f docker-compose.yml -f docker-compose.local.yml up --build` (see `docker-dev`)

## Ports
| Context | API | Frontend | DB |
|---|---|---|---|
| Manual dev | `https://localhost:7179` (`http://localhost:5202`) | `http://localhost:4201` (`:4200` if run directly from `bikontrol-web/`) | local Postgres |
| Docker local | `127.0.0.1:8080` | `127.0.0.1:4200` | `127.0.0.1:5433` |

## AI Setup
- `.opencode/` is the AI home (tracked in git): `skills/` (task playbooks in `SKILL.md` format), `agent/` (per-area playbooks: backend, frontend, reviewer), `command/` (shortcuts: /test, /migrate). Local plugin scaffold (`node_modules`, `package.json`) is ignored.
- `opencode.json` holds instructions, MCP servers and permissions. Skills, agents and commands need no config — opencode auto-discovers `.opencode/`.
- `AGENTS.md` is the single source of truth; `docs/specs/` holds details.

## Working Rules For This Repo
- Prefer small, focused changes.
- Keep API contracts, frontend types, and tests aligned in the same pass.
- EF migrations live in `Bikontrol.Persistence`; never edit applied migrations — use `npm run db:migration:add -- <Name>` then `npm run db:update`.
- Prefer the root scripts (`npm run test/build/db:*`) over per-folder commands — they are the CI parity layer.
- Never commit secrets: JWT key lives in `.env` / `appsettings.Development.json` only.
- Keep the root README and this file synchronized when behavior changes.
