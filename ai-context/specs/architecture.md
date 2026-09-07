# Architecture

## Traffic Flow
```
Internet → gateway (nginx) → bikontrol (Angular, :80)
                          → bikontrol-api (.NET, :8080) → bikontrol-db (PostgreSQL)
```

- `bikontrol-net` (external, shared with the gateway): `bikontrol` + `bikontrol-api`.
- `bikontrol-internal-net` (internal): database only, never on the shared network.

## Backend Layers (Clean Architecture)
- `Bikontrol.API`: controllers, middleware, HTTP surface
- `Bikontrol.Application`: DTOs, interfaces, validators, use-case contracts
- `Bikontrol.Infrastructure`: service implementations, JWT generation, AutoMapper profile
- `Bikontrol.Persistence`: `AppDbContext`, repositories, entity configuration
- `Bikontrol.Domain`: core entities
- `Bikontrol.Shared`: reusable exceptions and shared primitives
