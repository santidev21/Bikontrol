# Architecture

## Traffic Flow
```
Internet → gateway (nginx) → bikontrol (Angular, :8080)
                          → bikontrol-api (.NET, :8080) → bikontrol-db (PostgreSQL)
```

- `bikontrol-net` (external, shared with the gateway): `bikontrol` + `bikontrol-api`.
- `bikontrol-internal-net` (internal): database only, never on the shared network.

## Docker Services (detail)
| Service | Description |
|---|---|
| `db` | PostgreSQL 16 (internal network only, loopback `:5434` locally) |
| `api` | .NET 10 API (`:8080`, `/health` liveness + `/ready` readiness, `/metrics` Prometheus metrics when enabled, waits for healthy DB) |
| `web` | Angular 22 via nginx (`:8080`, loopback `:4200` locally) |

## Backend Layers (Clean Architecture)
Dependencies point inward: `API` → `Infrastructure` / `Persistence` → `Application` → `Domain` / `Shared`.
`ArchitectureTests` pins this (Application and Domain must not reference EF, Npgsql, QuestPDF,
Google, MailKit, WebPush or ASP.NET; Infrastructure and Persistence must not reference each other).

- `Bikontrol.API`: controllers, middleware, HTTP surface; the composition root (`AddApplication`,
  `AddInfrastructure`, `AddPersistence`)
- `Bikontrol.Application`: **use-case services** (`Services/`), DTOs, Mapperly mappers (`Mapping/`),
  the ports the services depend on (`Interfaces/`: repositories, `IEmailSender`, `IJwtTokenGenerator`,
  `IGoogleTokenValidator`, `IMaintenanceBookPdfRenderer`, `IDistributedLock`, `ITransactionManager`)
- `Bikontrol.Infrastructure`: adapters to the outside world only: SMTP, Web Push, JWT, password
  hashing, Google token validation, PDF rendering, background jobs, `CurrentUserService` (HTTP context)
- `Bikontrol.Persistence`: `AppDbContext`, repositories, entity configuration, migrations, advisory
  lock, demo seeding
- `Bikontrol.Domain`: core entities
- `Bikontrol.Shared`: reusable exceptions and shared primitives
