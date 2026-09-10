---
name: backend-test
description: Build and run the Bikontrol .NET backend tests. Use when running backend tests, dotnet build, dotnet test, Bikontrol.Tests, or checking backend coverage.
---

# Backend tests (.NET 8)

Solution: `Bikontrol/Bikontrol.sln`. Tests use xUnit. Prefer the root scripts (CI parity layer), from the repo root:

```bash
npm run build:api   # = dotnet build Bikontrol/Bikontrol.sln
npm run test:api    # = dotnet test Bikontrol/Bikontrol.sln
```

Raw form (from the repo root):

```bash
dotnet test Bikontrol/Bikontrol.sln --collect:"XPlat Code Coverage"
```

Rules:
- When you change an API contract, update the Angular types/services and tests in the same pass (see `api-contract`).
