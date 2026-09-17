---
name: angular-best-practices
description: Angular 19 conventions and quality rules for Bikontrol. Use when writing or reviewing components, services, guards, interceptors, pipes, templates, RxJS streams, routing, forms, or state in bikontrol-web/.
---

# Angular 19 best practices (Bikontrol)

Applies to `bikontrol-web/`. Angular 19, standalone components, Tailwind + SCSS, PWA, Jest.

## Components
- Standalone only (`standalone: true`), no `NgModule`. Import what the template uses.
- One component per folder: `.ts` + `.html` + `.scss` (+ `.spec.ts`).
- Prefer `input()` / `output()` signal APIs for new code; the existing code uses `@Input`/`@Output` — match the file you touch, don't mass-migrate.
- Use `inject()` in functional contexts (guards, interceptors). In classes, follow the surrounding file's style (repo uses constructor DI).
- `@Input()` must be typed. Never `any` — use the interfaces in `interfaces/*.interface.ts` (e.g. `Motorcycle`, not `any`).
- Prefer `OnPush` change detection for new presentational components; keep local state in class fields or signals.

## Templates
- Prefer built-in control flow (`@if`, `@for`, `@switch`) for new code; always add `track` to `@for`. Legacy `*ngIf`/`*ngFor` is acceptable in existing files.
- No business logic in templates — move it to typed getters/methods (see `StatisticsComponent`).
- Never bind untrusted HTML (`[innerHTML]`, `bypassSecurityTrust*`) without sanitizing.
- Show inline validation errors on forms; keep Spanish user-facing copy.

## Services & HTTP
- Components never call `HttpClient` directly — go through a service in `service/` or `shared/services`.
- Every service method returns a typed `Observable<T>` using an interface from `interfaces/`.
- Build URLs from `environment.apiUrl` via the `@env/environment` alias, never hardcode hosts.
- Central services to reuse: `AuthService`, `MotorcyclesService`, `MaintenanceService`, `StatisticsService`, `UserService`, `HttpErrorService`, `SwalService`, `UpdateService`.
- Auth is injected by `authInterceptor`; token refresh is handled by `refreshInterceptor` (order matters in `app.config.ts`).

## Errors & UX
- Surface failures with `SwalService` and translate them through `HttpErrorService.message(err, fallback)`. Never `console.error` as the only handling.
- Guard async UI with an `isLoading` flag and render a loading/skeleton state.

## RxJS
- `subscribe()` inside components is the current pattern, but **always** unsubscribe: `takeUntilDestroyed(this.destroyRef)` (preferred) or an explicit `ngOnDestroy` + `Subject`.
- Prefer the `async` pipe in templates for streams you don't need to mutate.
- Use `catchError`/`finalize` instead of nesting error logic; keep streams declarative.
- Never call `subscribe()` inside another `subscribe()` — use `switchMap`/`concatMap`.

## Routing
- Lazy-load every page with `loadComponent`. Functional guards/interceptors only.
- Dashboard children live under `dashboard` with a `**` fallback to `home`; keep the defensive fallbacks in place.
- Protect authenticated areas; `guestGuard` keeps logged-in users out of `/login` and `/register`.

## Forms
- Preferred: reactive forms (`FormBuilder` + `Validators`) with inline error messages for anything with more than a field or two.
- Validate on the client for UX, but never assume it replaces server-side validation.

## PWA
- Service worker config lives in `ngsw-config.json`; do not cache sensitive API responses (`/api/**`).
- Bumping the app version invalidates caches — rely on `UpdateService`, don't hardcode cache busting.

## Tests
- Jest, colocated `.spec.ts` next to the unit. Run `npm run test:ui` from the repo root.
- Existing specs often instantiate the class directly with mocks (`new Component(...)`) and `jest.mock('sweetalert2')`. Reuse that style.
- Test success **and** error paths for anything hitting a service.

## Don't
- Don't upgrade `@angular/*` or TypeScript versions as part of a feature change.
- Don't add a state library — the app uses services + local component state.
- Don't add `console.log`/`console.error`; route diagnostics through `HttpErrorService`/`SwalService`.

## Review checklist
- [ ] Standalone, typed inputs, no `any` for domain data.
- [ ] HTTP only via typed services; error handling through `SwalService` + `HttpErrorService`.
- [ ] Subscriptions unsubscribed; no nested subscribes.
- [ ] Lazy routes + guards preserved; new protected routes guarded.
- [ ] Inline validation errors; Spanish copy.
- [ ] Colocated jest spec covering the new/changed behavior; `npm run test:ui` green.
