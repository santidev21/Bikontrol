---
name: frontend-test
description: Build and run the Bikontrol Angular unit tests. Use when running frontend tests, npm test, vitest, ng test, Angular build, or checking frontend coverage.
---

# Frontend tests (Angular 22 + Vitest)

Prefer the root scripts, from the repo root:

```bash
npm run test:ui           # = ng test --watch=false (from bikontrol-web/)
npm run test:ui:coverage  # same tests + coverage gate enforced in CI
npm run build:ui          # production build
```

Raw form (from `bikontrol-web/`):

```bash
npm test                # ng test --watch=false
npm run test:coverage   # ng test --watch=false --configuration=coverage (gate)
npm run test:watch      # ng test (watch mode)
npm run build
```

Runner: Angular's own `@angular/build:unit-test` builder with **Vitest** (no Jest
config anymore). The runner initializes TestBed automatically and runs **zoneless**
(no `zone.js`). Global setup lives in `src/setup-vitest.ts` (jsdom polyfills:
`IntersectionObserver`, `matchMedia`).

## Coverage gate

Coverage is configured in `bikontrol-web/angular.json` under the `test`
configuration `coverage`, using `@vitest/coverage-v8`:

- **Scope**: all of `src/**/*.ts` (untested files count as 0%, so new files cannot
  get a free pass), excluding specs, `src/main.ts`, `src/setup-vitest.ts`,
  `src/environments/**`, app bootstrap (`app.config.ts`, `app.routes.ts`) and
  type-only `*.interface.ts` / `auth` model files. Component templates (`.html`)
  are **not** counted; flows are covered by unit specs where valuable and by E2E
  later.
- **Thresholds** (`statements`/`branches`/`functions`/`lines`) live in
  `angular.json` and fail `npm run test:coverage` when not met. They are a
  **ratchet**: raise them when coverage improves, never lower them to pass.
- Reports are written to `bikontrol-web/coverage/` (`text-summary`,
  `json-summary`, `cobertura`, `html`).

Rules:
- Write `.spec.ts` colocated with the unit under test; use `vi.fn()`/`vi.spyOn()`.
- `vi.mock` with **relative** paths is not supported by the runner — mock via
  Angular **TestBed** `providers` (e.g. constructor-injected services) instead.
- Root runner forces `http://localhost:4201`; direct `npm start` from `bikontrol-web/` uses `:4200`.
