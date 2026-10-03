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
- PR1 only *records* (channel `Pending`). Email (PR2) and web push (PR3) read those rows and mark
  them delivered, so the engine stays side-effect-free for users.

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
| `Reminders:DailyHourUtc` | `8` | Hour (UTC) the engine runs. |
| `Reminders:DedupeDays` | `3` | Days before the same item can remind again. |
