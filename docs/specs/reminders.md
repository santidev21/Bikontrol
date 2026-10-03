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
- Right after generating, the same run sends **one digest email per user** with all their pending
  reminders (`ReminderService.SendPendingEmailsAsync`), then marks those rows as channel `Email` with
  `DeliveredAt`. Set `Reminders:EmailEnabled=false` to record without emailing. Email delivery needs
  SMTP configured (see [auth.md](auth.md)); without SMTP the sender logs and does not deliver.
- Web push (PR3) reads any still-`Pending` rows the same way.

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
| `Reminders:DailyHourUtc` | `8` | Hour (UTC) the engine runs. |
| `Reminders:DedupeDays` | `3` | Days before the same item can remind again. |
