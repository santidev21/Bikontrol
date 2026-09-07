# Bikontrol — Production Deployment

Bikontrol is served at `https://bikontrol.santidev21.tech/` behind the `vps-gateway` reverse proxy. Deploys happen automatically on push to `main` via GitHub Actions.

## Architecture

```
Internet → gateway (nginx) → bikontrol (Angular, :80)
                          → bikontrol-api (.NET, :8080) → bikontrol-db (PostgreSQL)
```

- `bikontrol-net` (external, shared with the gateway): `bikontrol` + `bikontrol-api`.
- `bikontrol-internal-net` (internal): database only. The DB is **never** on the shared network.

## One-time VPS setup

```bash
git clone <repo> /opt/bikontrol
cd /opt/bikontrol
cp .env.example .env   # fill in real values, no CHANGE_ME left
# create the shared networks (or let the gateway's init-networks.sh do it)
docker network create bikontrol-net
docker network create bikontrol-internal-net
```

## Deploy

Automatic on push to `main` via GitHub Actions, or manual:

```bash
cd /opt/bikontrol && ./scripts/deploy.sh deploy
```

## Gateway integration

1. Create DNS A record `bikontrol.santidev21.tech` → VPS IP.
2. Issue the certificate (see `vps-gateway/AGENTS.md`).
3. Copy `deploy/bikontrol.santidev21.tech.conf` into the gateway's `sites-enabled/` and add `bikontrol-net` to the gateway `docker-compose.yml` networks and `init-networks.sh`.
4. Reload: `docker exec gateway nginx -t && docker exec gateway nginx -s reload`.

## Required secrets (`.env`, never committed)

| Variable | Purpose |
|---|---|
| `POSTGRES_DB` / `POSTGRES_USER` / `POSTGRES_PASSWORD` | Database credentials |
| `ConnectionStrings__DefaultConnection` | Full Npgsql connection string |
| `Jwt__Key` | JWT signing key (generate with `openssl rand -base64 48`) |
| `Jwt__Issuer` / `Jwt__Audience` | JWT issuer/audience (defaults provided) |
| `Cors__AllowedOrigins` | Comma-separated browser origins |
