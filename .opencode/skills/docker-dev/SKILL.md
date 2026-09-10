---
name: docker-dev
description: Run Bikontrol locally with Docker Compose. Use when starting services with docker compose, docker-compose.local.yml, bikontrol-db, bikontrol-api, bikontrol web, or debugging local ports.
---

# Local Docker

Run from the repo root:

```bash
# Full local stack (bridge networks, loopback debug ports)
docker compose -f docker-compose.yml -f docker-compose.local.yml up --build

# Validate only
docker compose config --quiet

# Tear down (data persists in bikontrol_db_data volume)
docker compose -f docker-compose.yml -f docker-compose.local.yml down
```

Local ports (loopback only):
- API: `127.0.0.1:8080`
- Web: `127.0.0.1:4200`
- DB: `127.0.0.1:5433`

Services (`docker-compose.yml`): `db` (PostgreSQL 16) → `api` (.NET 8, waits for healthy DB) → `web` (Angular via nginx). DB lives on `bikontrol-internal-net` only — never on the shared network.

Rules:
- Manual (no-Docker) dev uses `npm run bikontrol` (UI `:4201` + API `:7179`) or single-side runners.
- Never bake secrets into images — `.env` is excluded via `.dockerignore`.
