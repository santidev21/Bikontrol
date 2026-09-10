---
name: frontend-test
description: Build and run the Bikontrol Angular unit tests. Use when running frontend tests, npm test, jest, Angular build, or checking frontend coverage.
---

# Frontend tests (Angular 18 + Jest)

Prefer the root scripts, from the repo root:

```bash
npm run test:ui    # = jest --passWithNoTests --runInBand (from bikontrol-web/)
npm run build:ui   # production build
```

Raw form (from `bikontrol-web/`):

```bash
npm test
npm run build
```

Rules:
- Tests are **Jest**, not karma (karma deps exist but the `test` script is jest). Write `.spec.ts` colocated with the unit under test.
- Root runner forces `http://localhost:4201`; direct `npm start` from `bikontrol-web/` uses `:4200`.
