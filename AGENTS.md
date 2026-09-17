# Bikontrol Project Context

This file is the working context for Bikontrol. Keep it updated when architecture, routing, scripts, or conventions change. See [docs/specs/](docs/specs/) for detail docs.

## Project Snapshot
Motorcycle tracking and maintenance app:
- Angular 20 frontend (SCSS, Tailwind CSS, PWA service worker; Jest tests) — views: home, motorcycle summary, maintenance catalog, statistics (`/dashboard/statistics`), profile (`/dashboard/profile`)
- .NET 8 backend with Clean Architecture (API, Application, Domain, Infrastructure, Persistence, Shared)
- PostgreSQL 16 via EF Core (DB always in Docker, loopback-only `:5434` locally; migrations in Persistence, applied via root `db:migrate`)
- JWT authentication (login/register/Google OAuth), sliding sessions with refresh tokens, per-user salt password hashing, password recovery via SMTP email, soft deletes
- Read-only statistics aggregation (`GET /api/statistics/summary`); profile endpoints (`GET/PUT /api/users/me`, `POST /api/users/me/password`); multi-step writes run in transactions (`ITransactionManager`); optimistic concurrency via Postgres `xmin`; CHECK constraints on km/intervals
- Root `package.json` orchestrates local dev (`dev`, `dev:ui/dev:api`, `db:*`, `docker:dev` scripts)

## Repository Layout
```text
Bikontrol/
├─ Bikontrol/       # .NET solution (Bikontrol.sln: API, Application, Domain, Infrastructure, Persistence, Shared, Tests)
├─ bikontrol-web/   # Angular 20 application (src/app)
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
Clean Architecture layers: `API` (controllers) → `Application` (services, DTOs; AutoMapper 12 pinned) → `Domain` (entities with soft deletes) → `Persistence` (EF Core, `DbContext`, migrations) + `Infrastructure` → `Shared`. In Development the API takes DB/JWT values from `.env` via the root scripts (fallback: `Bikontrol.API/appsettings.Development.json`, gitignored, created from the committed `.example` template).

## Frontend Architecture
Angular 20 SPA in `bikontrol-web/src/app` (Tailwind + SCSS, PWA via `ngsw-config.json`, SweetAlert2 dialogs). Tests are Jest (`npm test` → `jest --passWithNoTests --runInBand`).

## Commands (run from repo root via root scripts unless noted)
- Both: `npm run dev` (DB in Docker + frontend + backend, hot reload) · `npm run build` · `npm run test` (see `/test`)
- Single side: `npm run dev:ui` · `npm run dev:api`
- Backend: `npm run test:api` (= `dotnet test Bikontrol/Bikontrol.sln`) · `npm run build:api` (see `backend-test` skill)
- Frontend: `npm run test:ui` · `npm run build:ui` (see `frontend-test`)
- Migrations: `npm run db:migration:add -- <Name>` (see `db-migrations`, or `/migrate`) · `npm run db:migrate` (= `dotnet ef database update …`)
- DB: `npm run db:up` (Postgres on `127.0.0.1:5434`, loopback-only) · `npm run db:down` · `npm run db:backup` / `db:restore` (compressed, 7-copy retention)
- Pre-deploy with real users: `npm run db:backup` first, then run `scripts/db-integrity-audit.sql` (read-only; every block must return 0 rows), then deploy (prod auto-applies migrations at startup)
- Docker: `npm run docker:dev` (= `docker compose -f docker-compose.yml -f docker-compose.local.yml up --build`) (see `docker-dev`)

## Ports
| Context | API | Frontend | DB |
|---|---|---|---|
| Native dev (`npm run dev`) | `https://localhost:7179` (`http://localhost:5202`) | `http://localhost:4201` (`:4200` if run directly from `bikontrol-web/`) | `127.0.0.1:5434` (docker, same volume) |
| Docker local (`npm run docker:dev`) | `127.0.0.1:8080` | `127.0.0.1:4200` | internal only |

## AI Setup
- `.opencode/` is the AI home (tracked in git): `skills/` (task playbooks in `SKILL.md` format, e.g. `api-contract`, `angular-best-practices`, `security-review`), `agent/` (playbooks: backend, frontend, reviewer, repo-auditor), `command/` (shortcuts: /test, /migrate, /review). Local plugin scaffold (`node_modules`, `package.json`) is ignored.
- Code review: use the `reviewer` agent for a diff/PR and the `repo-auditor` agent for a whole-repo, graded quality + security report. `/review [repo|all]` orchestrates either; both are read-only and confirm findings against `npm run test`.
- `opencode.json` holds instructions, MCP servers and permissions. Skills, agents and commands need no config — opencode auto-discovers `.opencode/`.
- `AGENTS.md` is the single source of truth; `docs/specs/` holds details.

## Working Rules For This Repo
- Prefer small, focused changes.
- Keep API contracts, frontend types, and tests aligned in the same pass.
- EF migrations live in `Bikontrol.Persistence`; never edit applied migrations — use `npm run db:migration:add -- <Name>` then `npm run db:migrate`.
- Prefer the root scripts (`npm run dev/db:*/build/test`) over per-folder commands — they are the CI parity layer; they inject `.env` values into the API so native dev matches the Docker DB.
- DB always runs in Docker (`npm run db:up`); raw compose ALWAYS uses both `-f` flags: `docker compose -f docker-compose.yml -f docker-compose.local.yml …`.
- Never commit secrets: JWT key lives in `.env` / `appsettings.Development.json` only (both gitignored).
- Keep the root README and this file synchronized when behavior changes.
