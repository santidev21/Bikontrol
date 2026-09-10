---
description: Run the full Bikontrol test suites (backend + frontend unit tests).
---

Prefer the root scripts (CI parity layer). From the repo root:

```bash
npm run test
```

Or per side:
```bash
npm run test:api   # = dotnet test Bikontrol/Bikontrol.sln (xUnit)
npm run test:ui    # = jest --passWithNoTests --runInBand (from bikontrol-web/)
```

Extra input: $ARGUMENTS (e.g. a test name filter or a single suite: `backend` / `frontend`).

Do not fix failures unless asked — report which suite and which test failed.
