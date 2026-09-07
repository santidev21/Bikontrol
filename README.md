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
- JWT authentication with login and register
- Soft deletes on core entities

---

## Tech stack
- Frontend: Angular 18, SCSS, Tailwind CSS, Jest
- Backend: .NET 8, Clean Architecture, EF Core, xUnit
- Database: PostgreSQL
- Authentication: JWT

---

## Architecture

Bikontrol is served at `https://bikontrol.santidev21.tech/` behind the `vps-gateway` reverse proxy:

```
Internet → gateway (nginx) → bikontrol (Angular, :80)
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
├─ ai-context/       # project context, playbooks, and AI notes
├─ docs/             # guides (deployment, etc.)
├─ scripts/          # orchestration scripts
└─ package.json      # root scripts for local dev and CI parity
```

---

## Getting Started

### Quick start
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

### Backend setup
The API loads `Bikontrol/Bikontrol.API/appsettings.Development.json` when `ASPNETCORE_ENVIRONMENT=Development`.

Before running the API for the first time on a new clone, generate a JWT secret and put it in that file:

```bash
openssl rand -base64 48
```

Put the generated value in `Jwt:Key` inside `Bikontrol/Bikontrol.API/appsettings.Development.json`. Keep `appsettings.json` for shared defaults only.

### Root scripts

| Command | Purpose |
| --- | --- |
| `npm run bikontrol` | Starts frontend and backend together |
| `npm run bikontrol-ui` | Starts only the Angular app |
| `npm run bikontrol-api` | Starts only the API |
| `npm run build` | Builds frontend and backend |
| `npm run test` | Runs frontend and backend tests |

### Useful backend paths
- API project: `Bikontrol/Bikontrol.API`
- Persistence project: `Bikontrol/Bikontrol.Persistence`
- Backend solution: `Bikontrol/Bikontrol.sln`

### Notes
- The root `package.json` is an orchestration layer; the frontend keeps its own Angular scripts inside `bikontrol-web/package.json`.
- Run `npm run bikontrol-ui` or `npm run bikontrol-api` when you want to work on only one side.
- Use `npm run db:update` after creating or receiving new EF migrations.
- Add a migration with `npm run db:migration:add -- YourMigrationName`.
- The API runner skips `launchSettings.json` and forces `https://localhost:7179` so it does not collide with the default HTTP port.
- The root frontend runner forces `http://localhost:4201` so it does not collide with the default Angular port.
- If you run Angular directly from `bikontrol-web/` with `npm start`, it still uses the default `4200` unless you pass a different port.

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

- JWT auth with a server-side signing key (stored in `.env` / `appsettings.Development.json`, never committed)
- Password hashing with a per-user salt
- Database isolated on an internal Docker network, never on the shared network
- Security headers (HTTPS, HSTS) applied by the gateway

---

## AI Context

[ai-context/](ai-context/) is the canonical project context for AI-assisted work (architecture snapshot, specs, agents, and skills).

---

## To Do

- [ ] Add Google OAuth authentication.
- [ ] Add password recovery on login.
- [ ] Bottom nav: pressing "Estadísticas" or "Perfil" redirects to login — it should do nothing (or go to home), not log the user out.
- [ ] Fix sessions expiring too frequently.
- [ ] Edit motorcycle: the km field shows 0 in Edit — it should show the current value but disabled.
- [ ] Allow uploading a custom motorcycle image.
- [ ] "Add custom maintenance" redirects to login (nonexistent route?).
- [ ] Predefined maintenance items don't appear — the DB migration was likely never run.
- [ ] Create a migrator that automatically applies new tables to the production DB.
- [ ] Create a read-only demo user (view-only, no edits) so people can try the app.
- [ ] Add the missing tests.
- [ ] DB backup and security.
- [ ] Add the statistics view.
- [ ] Add the profile view.
