---
description: Backend playbook for Bikontrol — .NET 8 Clean Architecture (API, Application, Domain, Infrastructure, Persistence). Use when working on Bikontrol/.
mode: subagent
---

# Backend agent

Playbook for .NET 8 backend work (`Bikontrol/`).

- Keep the Clean Architecture layering: controllers stay thin, logic goes in `Application` services, EF Core stays in `Persistence`.
- DTOs map via AutoMapper (pinned to `12.0.1` — do NOT upgrade; v15+ needs a paid license and breaks the net8.0 JWT stack).
- Entities use soft deletes — never hard-delete core records.
- Validate server-side on every input endpoint.
- Migrations live in `Bikontrol.Persistence` — never edit an applied migration.
- Prefer the root scripts (`npm run test:api`, `npm run db:update`); the API runner skips `launchSettings.json` and forces `https://localhost:7179`.
- When you change an API contract, update the Angular types/services and tests in the same pass.
