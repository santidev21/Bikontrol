# Bikontrol Project Context

This file is the working context for Bikontrol. Keep it updated when architecture, routing, scripts, or conventions change. See [docs/specs/](docs/specs/) for detail docs and [docs/adr/](docs/adr/) for decisions.

## Project Snapshot
Motorcycle tracking and maintenance app:
- Angular 22 frontend (SCSS, Tailwind CSS, PWA service worker; Vitest tests) — views: onboarding (`/dashboard/onboarding`), home, motorcycle summary, maintenance catalog, statistics (`/dashboard/statistics`), profile (`/dashboard/profile`)
- .NET 8 backend with Clean Architecture (API, Application, Domain, Infrastructure, Persistence, Shared)
- PostgreSQL 16 via EF Core (DB always in Docker, loopback-only `:5434` locally; migrations in Persistence, applied via root `db:migrate`)
- JWT authentication (login/register/Google OAuth), sliding sessions with refresh tokens, per-user salt password hashing, password recovery via SMTP email, soft deletes
- Read-only statistics aggregation (`GET /api/statistics/summary`); profile endpoints (`GET/PUT /api/users/me`, `POST /api/users/me/password`); multi-step writes run in transactions (`ITransactionManager`); optimistic concurrency via Postgres `xmin`; CHECK constraints on km/intervals
- Maintenance reminders: a daily background job flags due/overdue items (shared countdown with the dashboard, deduped) and delivers them by digest email + Web Push; `GET /api/reminders/due`, push subscribe endpoints and `PUT /api/users/me/reminders` (see [docs/specs/reminders.md](docs/specs/reminders.md) and [ADR 001](docs/adr/001-web-push.md)).
- Root `package.json` orchestrates local dev (`dev`, `dev:ui/dev:api`, `db:*`, `docker:dev` scripts)

## Product Direction
Bikontrol is the **primary product**: a B2C app for motorcycle owners (not workshops — no multi-tenancy). Priority is a sellable, trustworthy app, so reliability, security and observability come before new features. The other repos (SplitIt, MyBudgetBot, portfolio, …) are frozen: dependency bumps and bug fixes only. The maintainer's roadmap lives in `~/.opencode/plan/bikontrol-super-pro-plan.md`.

## Repository Layout
```text
Bikontrol/
├─ Bikontrol/       # .NET solution (Bikontrol.sln: API, Application, Domain, Infrastructure, Persistence, Shared, Tests)
├─ bikontrol-web/   # Angular 22 application (src/app)
├─ scripts/         # Orchestration scripts (run-bikontrol.mjs)
├─ deploy/          # Deployment configs
├─ docker/          # Dockerfiles (api, web)
├─ docs/            # Guides, specs, ADRs (docs/specs/, docs/adr/)
├─ .opencode/       # AI home: agent/, command/, skills/ (tracked; local plugin scaffold ignored)
├─ .github/         # CI/CD workflows
├─ docker-compose.yml
├─ docker-compose.local.yml
├─ package.json     # Root orchestration scripts
├─ opencode.json    # opencode config: instructions, MCP, permissions
└─ .env.example
```

## Backend Architecture
Clean Architecture layers: `API` (controllers) → `Application` (services, DTOs; AutoMapper 12 pinned) → `Domain` (entities with soft deletes) → `Persistence` (EF Core, `DbContext`, migrations) + `Infrastructure` → `Shared`. In Development the API takes DB/JWT values from `.env` via the root scripts (fallback: `Bikontrol.API/appsettings.Development.json`, gitignored, created from the committed `.example` template). Repositories never persist on their own — services (or `ITransactionManager`) call `SaveChangesAsync`; integration tests (`Bikontrol.Tests.Integration`, Testcontainers + Postgres) pin the write paths because the unit fakes do not persist.

## Frontend Architecture
Angular 22 SPA in `bikontrol-web/src/app` (Tailwind + SCSS, PWA via `ngsw-config.json`, SweetAlert2 dialogs). Tests are Vitest via Angular's `@angular/build:unit-test` builder (`npm test` → `ng test --watch=false`), zoneless.

## Quality Gates & Delivery
- Backend line coverage gate **80%** (`node scripts/check-coverage.mjs 80`, merged cobertura). Frontend coverage gate via Vitest v8 (`npm run test:ui:coverage`; thresholds in `bikontrol-web/angular.json`) scoped to TS logic (templates/bootstrap excluded) and ratcheted up over time — current baseline ~74% lines.
- SonarCloud (`santidev21_Bikontrol`) is a **required** status check (Quality Gate on new code).
- Mutation testing: Stryker.NET on `MaintenanceScheduleCalculator` (`stryker-config.json`, break **75**) and on `MaintenanceService` (`stryker-maintenance-service.json`, break **90**), via `.github/workflows/mutation.yml`.
- API contract: `ApiContractTests` regenerates `docs/api/openapi.json` (routes/requests) and `docs/api/dto-contract.json` (all DTO shapes) and fails CI when they change without the client. Refresh with `npm run api:contract:update` (see `docs/specs/api.md`).
- E2E: Playwright drives the critical flow (register → motorcycle → maintenance → record → overdue) against the real stack (`e2e/`, `.github/workflows/e2e.yml`; `npm run test:e2e`).
- CI (`ci.yml`): backend + frontend tests, **frontend production build** (AOT + env generator), Gitleaks, Trivy fs, CodeQL, SonarCloud, dependency audit (non-blocking), compose validation and deploy. E2E runs in `.github/workflows/e2e.yml`.
- Deploy (`scripts/deploy.sh`): backup + verify → integrity audit → build → up → container healthcheck → **post-deploy smoke test**; rolls back automatically on either failure. Staging reuses it via `DEPLOY_DIR`/`SMOKE_URL`.
- `main` is protected: required status checks, no force-push.

## Commands (run from repo root via root scripts unless noted)
- Both: `npm run dev` (DB in Docker + frontend + backend, hot reload) · `npm run build` · `npm run test` (see `/test`)
- Single side: `npm run dev:ui` · `npm run dev:api`
- Backend: `npm run test:api` (= `dotnet test Bikontrol/Bikontrol.sln`, runs unit + integration; integration needs Docker and auto-skips without it) · `npm run build:api` (see `backend-test` skill)
- API contract: `npm run api:contract:update` (regenerate `docs/api/*.json` after an intentional contract change)
- Frontend: `npm run test:ui` (= `ng test --watch=false`, Vitest) · `npm run test:ui:coverage` (= coverage gate in CI) · `npm run build:ui` (see `frontend-test`)
- E2E: `npm run test:e2e` (Playwright; needs the stack up — see `e2e/README.md`)
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

## Documentation Policy

Docs capture decisions and current state, never session narration.

- **Allowed:** `README` (how to run), `docs/adr/NNN-*.md` (one decision: context, options, decision,
  consequences), `docs/specs/*.md` (current design and business rules), runbooks (`DEPLOYMENT`, …)
  and any current audit/security doc.
- **Forbidden:** phase reports, progress logs, "what I did" narration and per-session summaries.
  When a change needs a durable record, update the relevant spec or add an ADR — do not create a
  report file. This applies to AI output too.

## Working Rules For This Repo
- Language: all code, comments, XML docs, tests, commit messages, PR titles/descriptions, docs (`README`, `docs/`, `AGENTS.md`), and AI output must be in English. Only user-facing UI strings may be in Spanish (via i18n files), never hardcoded Spanish in code/comments.
- Prefer small, focused changes.
- Keep API contracts, frontend types, and tests aligned in the same pass.
- EF migrations live in `Bikontrol.Persistence`; never edit applied migrations — use `npm run db:migration:add -- <Name>` then `npm run db:migrate`.
- Prefer the root scripts (`npm run dev/db:*/build/test`) over per-folder commands — they are the CI parity layer; they inject `.env` values into the API so native dev matches the Docker DB.
- DB always runs in Docker (`npm run db:up`); raw compose ALWAYS uses both `-f` flags: `docker compose -f docker-compose.yml -f docker-compose.local.yml …`.
- Never commit secrets: JWT key lives in `.env` / `appsettings.Development.json` only (both gitignored).
- Keep the root README and this file synchronized when behavior changes.
