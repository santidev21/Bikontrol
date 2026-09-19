---
name: frontend-test
description: Build and run the Bikontrol Angular unit tests. Use when running frontend tests, npm test, vitest, ng test, Angular build, or checking frontend coverage.
---

# Frontend tests (Angular 22 + Vitest)

Prefer the root scripts, from the repo root:

```bash
npm run test:ui    # = ng test --watch=false (from bikontrol-web/)
npm run build:ui   # production build
```

Raw form (from `bikontrol-web/`):

```bash
npm test           # ng test --watch=false
npm run test:watch # ng test (watch mode)
npm run build
```

Runner: Angular's own `@angular/build:unit-test` builder with **Vitest** (no Jest
config anymore). The runner initializes TestBed automatically and runs **zoneless**
(no `zone.js`). Global setup lives in `src/setup-vitest.ts` (jsdom polyfills:
`IntersectionObserver`, `matchMedia`).

Rules:
- Write `.spec.ts` colocated with the unit under test; use `vi.fn()`/`vi.spyOn()`.
- `vi.mock` with **relative** paths is not supported by the runner — mock via
  Angular **TestBed** `providers` (e.g. constructor-injected services) instead.
- Root runner forces `http://localhost:4201`; direct `npm start` from `bikontrol-web/` uses `:4200`.
