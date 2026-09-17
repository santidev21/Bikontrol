# Architecture

## Traffic Flow
```
Internet → gateway (nginx) → bikontrol (Angular, :80)
                          → bikontrol-api (.NET, :8080) → bikontrol-db (PostgreSQL)
```

- `bikontrol-net` (external, shared with the gateway): `bikontrol` + `bikontrol-api`.
- `bikontrol-internal-net` (internal): database only, never on the shared network.

## Docker Services (detail)
| Service | Description |
|---|---|
| `db` | PostgreSQL 16 (internal network only, loopback `:5434` locally) |
| `api` | .NET 8 API (`:8080`, health at `/health`, waits for healthy DB) |
| `web` | Angular 19 via nginx (`:80`, loopback `:4200` locally) |

## Backend Layers (Clean Architecture)
- `Bikontrol.API`: controllers, middleware, HTTP surface
- `Bikontrol.Application`: DTOs, interfaces, validators, use-case contracts
- `Bikontrol.Infrastructure`: service implementations, JWT generation, AutoMapper profile
- `Bikontrol.Persistence`: `AppDbContext`, repositories, entity configuration
- `Bikontrol.Domain`: core entities
- `Bikontrol.Shared`: reusable exceptions and shared primitives
