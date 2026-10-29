# Reminders

Maintenance reminders are how Bikontrol keeps a bike serviced without the owner doing mental math.
The engine reuses the **same countdown** the dashboard shows (`MaintenanceScheduleCalculator`), so a
reminder can never disagree with the app.

## What counts as "due"

An item is due for a reminder when it is **overdue** or has **≤ 20 %** of its life left — the same
threshold as the dashboard's "Próximo" bucket. Km and time items use the same rule:

- **Km**: `remaining = interval − (currentKm − lastPerformedKm)`; due when `remaining ≤ 0` or the life
  percentage is ≤ 20.
- **Time**: `remaining = intervalDays − daysSinceBaseline`; same rule, baseline = last record date or
  the motorcycle's first km entry.

## Engine (records, daily)

- A background service (`ReminderBackgroundService`) runs the engine **once a day** at
  `Reminders:DailyHourUtc` (default **08:00 UTC**). `Reminders:Enabled=false` turns it off.
- `ReminderService.GenerateDueRemindersAsync` finds every due item for users with reminders enabled
  (the read-only demo tenant is excluded) and records a **`reminder_logs`** row with the item's state
  at that moment (overdue, life %, remaining km/days).
- **Dedupe:** an item that already produced a `Due` reminder within `Reminders:DedupeDays`
  (default **3 days**) is skipped, so a chronically overdue item does not nag every day.
- **Per-channel delivery.** Each log tracks its own `EmailDeliveredAt` and `PushDeliveredAt`, so a
  reminder can go out on both channels and one channel never starves the other. `Channel` /
  `DeliveredAt` hold the *first* channel that delivered (`Pending` until then). Only logs created
  within the last `min(Reminders:DedupeDays, 2)` days are still delivered, so a stale log never goes
  out as a late "new" reminder.
- Right after generating, the same run sends **one digest email per user** with all their
  undelivered reminders (`ReminderService.SendPendingEmailsAsync`). Set `Reminders:EmailEnabled=false`
  to record without emailing. Email delivery needs SMTP configured (see [auth.md](auth.md)); without
  SMTP the sender logs and does not deliver. The digest is multipart (text + HTML) and carries a signed
  one-click unsubscribe (`List-Unsubscribe` headers + `POST /api/reminders/email/unsubscribe`, which
  turns `RemindersEnabled` off; see [ADR 004](../adr/004-email-delivery.md)).
- **Web Push** then delivers the logs not yet pushed to the user's browser subscriptions. It is a
  no-op without VAPID keys, and gone subscriptions (404/410) are deleted. See
  [../adr/001-web-push.md](../adr/001-web-push.md).
- **Failure isolation.** Delivery is persisted per user right after each send and a failure for one
  user is logged and skipped, so a retry never re-sends to users that already received it.
- **Multi-instance safety.** Both daily jobs take a Postgres advisory lock (`IDistributedLock`,
  `bikontrol:job:reminders` / `bikontrol:job:retention`) so only one API instance runs a job at a
  time. A run only counts as done when it finishes without error; a failed run retries on the next
  15-minute tick, and the engine is idempotent (dedupe + per-channel delivery flags).

## Web Push

- Subscriptions live in `push_subscriptions` (one row per endpoint; re-subscribe upserts keys).
- The browser subscribes with `SwPush` (Angular service worker) against the VAPID **public** key from
  `GET /api/reminders/push/vapid-public-key`; the server signs with the **private** key.
- Payload shape is the one Angular's SW expects: `{ notification: { title, body, data: { onActionClick } } }`.
- Enable/disable per device from **Perfil** ("Activar/desactivar notificaciones push").

## Preferences

- Every account has `RemindersEnabled` (default **true**). Disable via
  `PUT /api/users/me/reminders` `{ "enabled": false }`.

## API

| Method | Route | Notes |
|---|---|---|
| `GET` | `/api/reminders/due` | Maintenance due/overdue for the current user, worst first. |
| `PUT` | `/api/users/me/reminders` | `{ enabled }` — turn reminders on/off. Disabled for demo accounts. |

## Config

| Key | Default | Purpose |
|---|---|---|
| `Reminders:Enabled` | `true` | Run the daily engine. |
| `Reminders:EmailEnabled` | `true` | Send the digest email (requires SMTP). |
| `Reminders:PushEnabled` | `true` | Attempt Web Push (no-op without VAPID keys). |
| `Reminders:DailyHourUtc` | `8` | Hour (UTC) the engine runs. |
| `Reminders:DedupeDays` | `3` | Days before the same item can remind again. |
| `WebPush:PublicKey` / `WebPush:PrivateKey` | *(empty)* | VAPID keys; push is disabled without them. |
| `WebPush:Subject` | `mailto:…` | Contact URI sent to the push service. |
