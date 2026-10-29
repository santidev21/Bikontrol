# API Routes

## Error envelope

- Errors: every non-2xx answer is `application/problem+json` (RFC 9457): `{ type: "urn:bikontrol:error:<code>", title, status, detail, code, error, traceId }`. `code` is the stable machine part; `error` repeats the Spanish message for the client contract (`{ error, code }`). `ExceptionHandlingMiddleware` maps types to `not_found`/`forbidden`/`validation`/`conflict`/`duplicate`/`payload_too_large`/`server_error`; the typed exceptions (`Auth`, `Validation`, `NotFound`, `Forbidden`) may carry a specific code (e.g. `invalid_credentials`, `session_expired`, `maintenance_no_access`, `km_below_previous`). Bare `401`/`403`/`404`, model-validation `400`s and rate-limit `429`s use the same envelope (`unauthorized`, `forbidden`, `not_found`, `validation`, `rate_limited`).
- The frontend `HttpErrorService` translates any code with an `errors.*` catalog entry (ES/EN) and falls back to the server message. For a new user-visible exception: throw with a code and add the `errors.<code>` entry to both translation catalogs.

## Limits and rate limiting

- Request bodies are capped by Kestrel (`Limits:MaxRequestBodyBytes`, default 3 MB; images travel as <= 1 MB data URLs) and answer `413 payload_too_large`.
- `auth` policy (per IP): 10/min burst + `RateLimiting:AuthPerHour` sustained. `heavy` (per user; exports, CSV/PDF maintenance book): `RateLimiting:HeavyPerMinute`, default 6. `upload` (per user; attachments, motorcycle create/update with images): `RateLimiting:UploadPerMinute`, default 20. Rejections are `429 rate_limited` with `Retry-After`. The limiter runs after authentication so per-user policies see the principal.
- Outbound calls are bounded: SMTP `Smtp:TimeoutSeconds` (default 15) and Web Push (10 s shared `HttpClient`), each retried up to 3 times with exponential backoff on transient failures only (network, timeouts, SMTP 4xx, push 429/5xx).

## Health
- `GET /health` — liveness: `200 Healthy` whenever the process is up (no dependency checks).
- `GET /ready` — readiness: `200 Healthy` only when PostgreSQL responds; `503` otherwise (own 3s-timeout connection).

## Auth
- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/google`
- `POST /api/auth/refresh`
- `POST /api/auth/forgot-password`
- `POST /api/auth/reset-password`
- `POST /api/auth/confirm-email`
- `POST /api/auth/resend-confirmation`
- `POST /api/auth/demo` (opt-in; `404` unless `Demo__Enabled=true`)

## Motorcycles
- `POST /api/motorcycles`
- `GET /api/motorcycles/mine`
- `GET /api/motorcycles/{id}`
- `PUT /api/motorcycles/{id}`
- `DELETE /api/motorcycles/{id}`
- `GET /api/motorcycles/{id}/km/current`
- `POST /api/motorcycles/{id}/km-history`
- `DELETE /api/motorcycles/{id}/km-history/last`
- `GET /api/motorcycles/{id}/maintenance-book/book.csv` (maintenance book as CSV)
- `GET /api/motorcycles/{id}/maintenance-book/book.pdf` (maintenance book as a shareable PDF)

## Maintenances
- `GET /api/maintenances/defaults`
- `GET /api/maintenances/mine`
- `GET /api/maintenances/mine/motorcycle/{motorcycleId}`
- `GET /api/maintenances/{id}`
- `POST /api/maintenances/mine`
- `POST /api/maintenances/follow`
- `PUT /api/maintenances/{id}`
- `DELETE /api/maintenances/mine/{id}`
- `POST /api/maintenances/records` (optional `cost`, ≥ 0; feeds cost statistics)
- `GET /api/maintenances/motorcycle/{motorcycleId}/records` (newest first; optional `?limit` 1–500 to page the history, default = full history — values over the ceiling clamp)
- `GET /api/maintenances/motorcycle/{motorcycleId}/upcoming`
- `GET /api/maintenances/records/{recordId}/attachments` (photos/documents of a record)
- `POST /api/maintenances/records/{recordId}/attachments` (`{ dataUrl, fileName }`; JPEG/PNG/WebP data URL, ≤1 MB decoded)
- `DELETE /api/maintenances/records/{recordId}/attachments/{attachmentId}`

## Reminders
- `GET /api/reminders/due` (maintenance due/overdue for the current user)
- `GET /api/reminders/push/vapid-public-key` (anonymous; `null` when push is off)
- `POST /api/reminders/push/subscribe` (`{ endpoint, keys: { p256dh, auth } }`)
- `POST /api/reminders/push/unsubscribe` (`{ endpoint }`)

## Statistics (read-only aggregation, demo allowed)
- `GET /api/statistics/summary` (includes cost aggregates: `totalCost`, `costPerKm`, `costByMotorcycle`, `costByYear` and per-month `cost`; fuel/cost-of-ownership: `totalFuelCost`, `totalCostOfOwnership`, `fuelCostPerKm`, `averageKmPerLitre`, `hasConsumptionDrop`; and `budget` — monthly/annual maintenance budget vs spend with `isMonthlyOverBudget`/`isAnnualOverBudget`)

## Fuel-ups (refuelling log + consumption)
- `POST /api/fuel-ups` (`{ motorcycleId, fueledAt, odometerKm, litres, totalCost, isFullTank, station?, notes? }`; advances the odometer when higher)
- `GET /api/fuel-ups/motorcycle/{motorcycleId}` (newest first; optional `?limit` 1–500 to page the log, default = full history — values over the ceiling clamp)
- `GET /api/fuel-ups/{id}`
- `PUT /api/fuel-ups/{id}` (same body)
- `DELETE /api/fuel-ups/{id}`
- `GET /api/fuel-ups/motorcycle/{motorcycleId}/consumption` (full-to-full km/l segments, fuel cost/km, total cost of ownership, consumption-drop flag)

## Users (profile)
- `GET /api/users/me`
- `PUT /api/users/me` (`{ fullName }`)
- `POST /api/users/me/password` (`{ currentPassword, newPassword }`; 400 for Google-only accounts)
- `PUT /api/users/me/reminders` (`{ enabled }`)
- `PUT /api/users/me/budget` (`{ monthlyMaintenanceBudget?, annualMaintenanceBudget? }`; `null` disables a budget)
- `GET /api/users/me/activity` (the current user's audit trail, newest first; `?limit` 1–200, default 50)
- `GET /api/users/me/export` (everything the current user owns as one JSON document: profile, motorcycles, maintenances, maintenance records, attachments and fuel-ups; "download my data")
- `POST /api/users/me/delete` (`{ confirmation: "ELIMINAR", password? }`; irreversible: purges owned data and anonymizes the account — see [ADR 003](../adr/003-account-deletion.md))

## Contract snapshots (client sync)

The API and the Angular client are kept in sync by committed snapshots, guarded by
`ApiContractTests` (runs in the required backend-tests check):

- `docs/api/openapi.json` — the OpenAPI document (routes, parameters, request bodies and
  validation), generated with the same configuration the app serves
  (`OpenApiExtensions.AddBikontrolOpenApi`, native OpenAPI 3.1). Because controllers return
  `IActionResult`, the generator cannot infer response types, so responses are covered by the
  file below.
- `docs/api/dto-contract.json` — the JSON shape (property names, types, required/optional) of
  every DTO in `Bikontrol.Application` and `Bikontrol.API`, including responses and nested objects.

Any route or DTO shape change fails CI until the snapshot is refreshed, forcing the client
update in the same pass. After reviewing the change, run `npm run api:contract:update` and
commit the diff. Update the Angular type/service and its `.spec.ts` in the same PR (see the
`api-contract` skill).
