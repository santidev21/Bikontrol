# Bikontrol E2E (Playwright)

End-to-end tests that drive the real stack (nginx + API + PostgreSQL) through a
browser. They complement the unit and integration suites by covering the wiring
those cannot see (routing, guards, the actual UI contract).

## Critical flow

`tests/critical-flow.spec.ts`: register → add motorcycle → create a maintenance
with a small Km interval → record it → advance the odometer → see it overdue.

## Run locally

The stack must serve the app with a **same-origin** `/api` (the production CSP
allows only `connect-src 'self'`). The `docker-compose.e2e.yml` override does
that, plus disables email confirmation so the suite can register without a mailbox.

```bash
# From the repo root, with an .env that has the DB/JWT values:
docker compose -f docker-compose.yml -f docker-compose.local.yml -f docker-compose.e2e.yml up -d --build
cd e2e
npm ci
npx playwright install chromium
E2E_BASE_URL=http://127.0.0.1:4300 npm test
```

The suite runs serially and registers a unique user per run, so it is safe to
re-run against a reused database.

## CI

`.github/workflows/e2e.yml` builds the stack, waits for readiness and runs the
suite. The HTML report and traces are uploaded as the `playwright-report`
artifact on every run.
