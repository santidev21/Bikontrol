---
name: api-contract
description: Keep the .NET API and Angular client in sync when a contract changes. Use when changing DTOs, controllers, endpoints, Angular models, services calling the API, or response shapes.
---

# API contract sync

Bikontrol has no codegen — contracts are synced by hand. When a backend DTO, controller route, or response shape changes, do all of these in the same pass:

1. Backend: DTO in `Application` (AutoMapper stays pinned to `12.0.1`), controller in `API`, server-side validation on the endpoint.
2. Frontend: matching TypeScript type/interface and the Angular service that calls the API.
3. Tests: backend test in `Bikontrol.Tests/` and frontend Jest `.spec.ts` covering the new shape.
4. Docs: update `docs/specs/` and `AGENTS.md` if behavior changed.

Checklist before finishing:
- `npm run build` (both sides) passes.
- Soft-delete convention preserved on core entities.
