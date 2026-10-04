# API Routes

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
- `POST /api/maintenances/records`
- `GET /api/maintenances/motorcycle/{motorcycleId}/records`
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
- `GET /api/statistics/summary`

## Users (profile)
- `GET /api/users/me`
- `PUT /api/users/me` (`{ fullName }`)
- `POST /api/users/me/password` (`{ currentPassword, newPassword }`; 400 for Google-only accounts)
- `PUT /api/users/me/reminders` (`{ enabled }`)
