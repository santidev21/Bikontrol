# Frontend agent

Playbook for Angular 18 frontend work (`bikontrol-web/`).

- Main areas: `src/app/shared` (services, components, interceptors), `src/app/modules/auth` (login/register), `src/app/modules/dashboard` (home, motorcycles, maintenance).
- Central services: `AuthService`, `MotorcyclesService`, `MaintenanceService`, `HttpErrorService`, `SwalService`.
- When you add a UI flow, add or update `*.spec.ts` unit tests in the same pass.
- Surface UI errors to the user via `SwalService`, never `console.error`.
