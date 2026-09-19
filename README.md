# Bikontrol

![.NET](https://img.shields.io/badge/.NET-8-purple)
![Angular](https://img.shields.io/badge/Angular-18-red)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-blue)

Bikontrol is a full-stack web app for motorcycle owners to track motorcycles, km history, and maintenance plans.

<!-- Hero screenshot -->
<!-- ![Bikontrol dashboard](docs/screenshots/dashboard.png) -->

---

## Features

- Manage your motorcycles (create, edit, disable)
- Track km history per motorcycle
- Maintenance plans with default and user-defined items
- Record completed maintenance
- Statistics dashboard (fleet km, maintenance health, activity)
- User profile (edit name, change password; Google accounts are password-less)
- JWT authentication with login and register
- Soft deletes on core entities

---

## Tech stack
- Frontend: Angular 22, SCSS, Tailwind CSS, Vitest
- Backend: .NET 8, Clean Architecture, EF Core, xUnit
- Database: PostgreSQL
- Authentication: JWT

---

## Architecture

Bikontrol is served at `https://bikontrol.santidev21.tech/` behind the `vps-gateway` reverse proxy:

```
Internet → gateway (nginx) → bikontrol (Angular, :8080)
                          → bikontrol-api (.NET, :8080) → bikontrol-db (PostgreSQL)
```

- `bikontrol-net` (external, shared with the gateway): `bikontrol` + `bikontrol-api`.
- `bikontrol-internal-net` (internal): database only. The DB is **never** on the shared network.

---

## Project Structure
```text
Bikontrol/
├─ Bikontrol/        # .NET solution and backend tests
├─ bikontrol-web/    # Angular application
├─ docs/             # guides (deployment, etc.) and specs (docs/specs/)
├─ scripts/          # orchestration scripts
├─ .opencode/        # AI home: agent/, command/, skills/
├─ package.json      # root scripts for local dev and CI parity
└─ opencode.json     # opencode config (instructions, MCP, permissions)
```

---

## Local Development

The database (PostgreSQL 16) **always runs in Docker** — loopback-only (`127.0.0.1:5434`), never exposed externally. Only where the app itself runs changes:

- `npm run docker:dev` → everything (DB + API + frontend) in Docker, closest to prod.
- `npm run dev` → DB in Docker, API + frontend native (`dotnet run` / `ng serve`) with hot reload, against the **same** `bikontrol_db_data` volume.

### Prerequisites

- Node.js 20+
- .NET 8 SDK
- Docker Desktop (running)

### 1. Environment setup

```bash
cp .env.example .env
# Fill POSTGRES_PASSWORD (avoid '$'; wrap '#' or spaces in single quotes),
# Jwt__Key (generate with: openssl rand -base64 48)
```

### 2. Run everything in Docker (closest to prod)

```bash
npm run docker:dev
# = docker compose -f docker-compose.yml -f docker-compose.local.yml up --build
```

- API: `http://127.0.0.1:8080` (health at `/health`)
- Web: `http://127.0.0.1:4200`
- DB: `127.0.0.1:5434` (loopback-only)
- Data persists in the `bikontrol_db_data` volume; `docker compose … down -v` wipes it.

### 3. Run natively with hot reload

```bash
npm run dev
```

Starts Postgres in Docker, then runs the frontend (`http://localhost:4201`) and the API (`https://localhost:7179`) together. On first run it creates `Bikontrol/Bikontrol.API/appsettings.Development.json` from the committed `.example` template — review `ConnectionStrings:DefaultConnection` (`127.0.0.1:5434`) and `Jwt:Key`.

Single side:

```bash
npm run dev:ui    # Angular app only (:4201)
npm run dev:api   # API only (:7179)
```

> If you run Angular directly from `bikontrol-web/` with `npm start`, it uses the default `:4200` unless you pass `--port 4201`.

### 4. Database & migrations

```bash
npm run db:up       # start Postgres only (127.0.0.1:5434, loopback-only)
npm run db:down     # stop it (data persists in the volume)
npm run db:migrate  # apply EF migrations (dotnet ef database update)
# New migration:
npm run db:migration:add -- YourMigrationName
```

Native `dotnet run` takes the DB credentials and JWT key from `.env`, so they always match the Docker Postgres.

### Commands

| Command | Purpose |
| --- | --- |
| `npm run dev` | DB (Docker) + frontend + backend with hot reload |
| `npm run dev:ui` / `npm run dev:api` | Frontend / API only |
| `npm run db:up` / `npm run db:down` | Start / stop Postgres in Docker |
| `npm run db:migrate` | Apply EF migrations |
| `npm run docker:dev` | Full stack in Docker (like prod) |
| `npm run build` | Build frontend and backend |
| `npm run test` | Run frontend and backend tests |

### Useful backend paths

- API project: `Bikontrol/Bikontrol.API`
- Persistence project: `Bikontrol/Bikontrol.Persistence`
- Backend solution: `Bikontrol/Bikontrol.sln`

### Notes

- The root `package.json` is the orchestration layer; the frontend keeps its own Angular scripts inside `bikontrol-web/package.json`.
- The API runner skips `launchSettings.json` and forces `https://localhost:7179` so it does not collide with the default HTTP port.
- The root frontend runner forces `http://localhost:4201` so it does not collide with the default Angular port.

### Gotchas

| Problem | Cause | Fix |
|---|---|---|
| Login returns `504 Gateway Timeout` but the API is healthy | Stale PWA service worker cached in the browser | Hard-refresh (`Ctrl+Shift+R`) or clear site data for `localhost:4200` |
| Users see an old version after a deploy | Normal PWA behavior | The app detects the new version and shows a "Nueva versión disponible" prompt (reload when ready). Check the served version in Perfil → Versión |
| `npm run dev` → API auth fails against Docker Postgres | `.env` `POSTGRES_PASSWORD` / `Jwt__Key` still `CHANGE_ME` | Fill `.env` (the root scripts inject it into the API) |

#### Known dependency notes
- `AutoMapper` is pinned to `12.0.1`. Versions `>= 15` require a paid license and pull .NET 9/10 + `Microsoft.IdentityModel` 8.x dependencies that conflict with the net8.0 JWT stack. The upstream advisory `GHSA-rvv3-g6hj-g44x` (DoS via deep recursive object graphs) does not apply here: Bikontrol only maps flat, fixed-shape DTOs with no recursive/self-referencing graphs reachable from user input. The advisory is suppressed in `Bikontrol/Directory.Build.props` with that rationale.

---

## Deployment

Deploys happen automatically on push to `main` via GitHub Actions. For VPS setup and manual deploy commands, see [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md).

---

## Screenshots

<!-- Add your screenshots here -->
<!-- ![Dashboard](docs/screenshots/dashboard.png) -->

---

## Security

- JWT auth with a server-side signing key (stored in `.env` / `appsettings.Development.json`, never committed) — now includes `role` claim (`User`/`Demo`)
- Sliding sessions: short-lived access token + long-lived refresh token (rotated on each use, stored hashed in the DB)
- Google OAuth "Sign in with Google" (ID-token flow; the Google Client ID is public, no Client Secret required)
- Demo user: read-only account (`demo@bikontrol.com`, `Role=Demo`) via `POST /api/auth/demo` + frontend one-click demo; write operations enforced server-side (403) and hidden in UI
- Password recovery via email (SMTP configured in `.env`; reset tokens are hashed and time-limited)
- Password hashing with a per-user salt
- Database isolated on an internal Docker network, never on the shared network; transport encrypted with TLS (`ssl=on` + self-signed, `SslMode=Require`)
- Automated DB backups: local `npm run db:backup`/`db:restore` (7-copy retention) + VPS `deploy.sh backup-db` in persistent `backups/` + cron example in `docs/DEPLOYMENT.md`
- Security headers (HTTPS, HSTS) applied by the gateway

---

## AI Context

- [AGENTS.md](AGENTS.md) — project snapshot (stack, layout, commands, working rules)
- [opencode.json](opencode.json) — instructions, MCP servers and permissions
- [.opencode/agent/](.opencode/agent/) — per-area playbooks (backend, frontend, reviewer, repo-auditor)
- [.opencode/skills/](.opencode/skills/) — task playbooks (migrations, tests, docker, contracts, angular, security)
- [.opencode/command/](.opencode/command/) — shortcuts (`/test`, `/migrate`, `/review`)
- [docs/specs/](docs/specs/) — architecture, auth, database detail specs

---

## To Do

- [x] Add Google OAuth authentication.
- [x] Add password recovery on login.
- [x] Bottom nav: pressing "Estadísticas" or "Perfil" redirects to login — dead links now point to home, plus a dashboard wildcard fallback so unknown dashboard URLs never land on login.
- [x] Fix sessions expiring too frequently.
- [x] Edit motorcycle: the km field shows the current value (from km history) and is disabled in Edit.
- [x] Allow uploading a custom motorcycle image (stored as a resized data URL in `Motorcycle.Image`).
- [x] "Add custom maintenance" redirects to login — the route existed and navigation was correct (verified by spec); likely a stale deployed bundle, plus the dashboard wildcard fallback now prevents this class of issue.
- [x] Predefined maintenance items don't appear — the `CleanupAllButUsers` migration had truncated the seeded `MaintenanceTypes` table; fixed with the idempotent `SeedPredefinedMaintenanceTypes` re-seed migration.
- [x] Create a migrator that automatically applies new tables to the production DB — already implemented: the API runs `db.Database.Migrate()` on startup in any non-Development environment (`Program.cs`), so production applies pending migrations automatically.
- [x] Create a read-only demo user (view-only, no edits) so people can try the app. — `POST /api/auth/demo` (auto-creates `demo@bikontrol.com` with `Role=Demo`), JWT carries `role` claim, write endpoints return 403 for Demo, frontend shows "Probar demo" button on login + modo solo lectura banner.
- [x] Add the missing tests. — 140 backend unit tests (interval/%-remaining matrix, write-path integrity, statistics, profile, demo guards, auth, motorcycle CRUD + mapping + DTO validation) + 5 integration tests (real API + PostgreSQL via Testcontainers, pinning the write paths) + 159 frontend Vitest tests (statistics/profile views, services, guards included).
- [x] DB backup and security. — `npm run db:backup` / `db:restore` (compressed, retention 7), `deploy.sh backup-db` in persistent `backups/`, Postgres SSL (`ssl=on` + self-signed, `SslMode=Require;Trust Server Certificate=true`). Multi-step writes run in transactions, optimistic concurrency via `xmin`, CHECK constraints on km/intervals, read-only audit in `scripts/db-integrity-audit.sql` (run it + a backup before every deploy with real users).
- [x] Add the statistics view. — `/dashboard/statistics` backed by read-only `GET /api/statistics/summary` (KPIs, maintenance health, km per bike, records by type, 6-month activity; hand-rolled SVG/CSS charts).
- [x] Add the profile view. — `/dashboard/profile` backed by `GET/PUT /api/users/me` + `POST /api/users/me/password` (name editable for all; password change only for password accounts).
