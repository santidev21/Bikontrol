---
description: Frontend playbook for Bikontrol — Angular 20 (Tailwind, SCSS, Jest). Use when working on bikontrol-web/.
mode: subagent
---

# Frontend agent

Playbook for Angular 20 frontend work (`bikontrol-web/`).

- Tailwind CSS + SCSS; PWA via `ngsw-config.json`; SweetAlert2 for dialogs.
- Main areas: `src/app/shared` (services, components, interceptors), `src/app/modules/auth` (login/register), `src/app/modules/dashboard` (home, motorcycles, maintenance).
- Central services: `AuthService`, `MotorcyclesService`, `MaintenanceService`, `HttpErrorService`, `SwalService`.
- Surface UI errors to the user via `SwalService`, never `console.error`.
- Tests are Jest (`npm test` → `jest --passWithNoTests --runInBand`), not karma — write `.spec.ts` colocated with the unit under test.
- Root runner forces `http://localhost:4201`; running `npm start` directly from `bikontrol-web/` uses default `:4200`.
- Show inline validation errors.
