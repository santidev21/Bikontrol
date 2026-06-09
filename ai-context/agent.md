# Bikontrol Project Context

This file is the working context for Bikontrol. Keep it updated when architecture, routing, scripts, or conventions change.

## Project Snapshot
Bikontrol is a motorcycle maintenance platform with:
- Angular 18 frontend
- .NET 8 backend using Clean Architecture
- PostgreSQL persistence
- JWT authentication
- Frontend tests with Jest
- Backend tests with xUnit

## Repository Layout
```text
/home/santidev21/Dev/Bikontrol
├─ Bikontrol/        # .NET solution, API, infrastructure, persistence, tests
├─ bikontrol-web/    # Angular application
├─ ai-context/       # project context and helper playbooks
├─ scripts/          # orchestration scripts
└─ package.json      # root dev commands
```

## Backend Architecture
### Layers
- `Bikontrol.API`: controllers, middleware, HTTP surface
- `Bikontrol.Application`: DTOs, interfaces, validators, use-case contracts
- `Bikontrol.Infrastructure`: service implementations, JWT generation, AutoMapper profile
- `Bikontrol.Persistence`: `AppDbContext`, repositories, entity configuration
- `Bikontrol.Domain`: core entities
- `Bikontrol.Shared`: reusable exceptions and shared primitives

### Core Backend Concepts
- Authentication uses JWT and password hashing.
- Users own motorcycles.
- Motorcycles can store km history.
- Maintenance has default types and user-defined maintenance items.
- The app supports soft deletes for several entities through `IsEnabled`.

### Main API Routes
- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/motorcycles`
- `GET /api/motorcycles/mine`
- `GET /api/motorcycles/{id}`
- `PUT /api/motorcycles/{id}`
- `DELETE /api/motorcycles/{id}`
- `GET /api/maintenances/defaults`
- `GET /api/maintenances/mine`
- `GET /api/maintenances/{id}`
- `POST /api/maintenances/mine`
- `POST /api/maintenances/follow`
- `PUT /api/maintenances/{id}`
- `DELETE /api/maintenances/mine/{id}`

## Frontend Architecture
### Main Areas
- `src/app/shared`: reusable services, components, interceptors
- `src/app/modules/auth`: login and register flows
- `src/app/modules/dashboard`: home, motorcycles, maintenance, and related cards/pages

### Frontend Services
- `AuthService`: login, register, logout, token storage
- `MotorcyclesService`: CRUD for motorcycles
- `MaintenanceService`: maintenance CRUD and defaults
- `HttpErrorService`: central error parsing for cleaner UI messages
- `SwalService`: notifications and user feedback

### Auth Flow
- Login and register store the JWT token in `localStorage`.
- `auth.interceptor.ts` adds `Authorization: Bearer <token>` automatically.
- UI errors should be surfaced to the user, not logged with `console.error`.

## Test Strategy
### Frontend
- Unit tests live next to the Angular code as `*.spec.ts` files.
- Focus areas:
  - login and register flows
  - token handling
  - motorcycle creation and editing
  - maintenance creation and editing
  - record maintenance flow
  - service error handling

### Backend
- Tests live under `Bikontrol/Bikontrol.Tests`.
- Focus areas:
  - auth service and JWT generation
  - auth controller behavior
  - motorcycles controller behavior
  - maintenances controller behavior
  - service-level business rules

## Root Commands
From `/home/santidev21/Dev/Bikontrol`:
- `npm run bikontrol` starts frontend and backend together.
- `npm run bikontrol-ui` starts only Angular.
- `npm run bikontrol-api` starts only the API.
- `npm run build` builds both sides.
- `npm run test` runs both test suites.

## Port Behavior
- Root orchestration forces the frontend to `http://localhost:4201`.
- The API runner forces `https://localhost:7179`.
- If you run Angular directly inside `bikontrol-web/`, the default port is still `4200` unless overridden.

## Working Rules For This Repo
- Prefer small, focused changes.
- Keep auth, motorcycles, and maintenance flows aligned across front and back.
- When you change an API contract, update frontend types and tests together.
- When you add a UI flow, add or update unit tests in the same pass.
- Keep the root README and this file synchronized when behavior changes.

## Current Notes
- The frontend already uses a cleaner shared error parser instead of scattered `console.error` calls.
- The backend test project is wired to the API project and can be expanded with more service and integration tests.
- Root orchestration currently uses port `4201` for Angular, while direct Angular runs still default to `4200`.
