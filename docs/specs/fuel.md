# Fuel, cost and consumption

Log fuel-ups (litres, price, odometer) per motorcycle and derive consumption
(km/l), fuel cost per km, total cost of ownership and a monthly/annual
maintenance budget with overspend flags. A sharp drop in consumption doubles as
an early sign of a mechanical problem.

## Model

- `FuelUps` (see [database.md](database.md)) — one row per refuelling, owned by a
  motorcycle. `IsFullTank` marks a fill to the top; only full tanks delimit a
  consumption segment.
- `users.MonthlyMaintenanceBudget` / `users.AnnualMaintenanceBudget` — optional
  per-user maintenance budgets (nullable; `null` disables the check).

## Consumption (full-to-full)

`FuelConsumptionCalculator` (Application, pure) walks the fuel-ups ordered by
odometer:

- The first full tank opens the baseline and produces no segment.
- Every subsequent full tank closes a segment: distance = odometer difference,
  litres = the sum of all fills since the previous full tank (so partial fills
  are folded in), `km/l = distance / litres`.
- Non-increasing odometer readings and segments without fuel are skipped.

Derived metrics: weighted `AverageKmPerLitre`, `FuelCostPerKm`,
`CostPerKm` (total cost of ownership = fuel + maintenance) and `TotalDistanceKm`.

## Consumption-drop signal

Once there are at least three segments, the latest `km/l` is compared with the
average of the previous (up to five) segments. A drop beyond 20% sets
`IsConsumptionDrop` — an early sign of a mechanical problem. The per-motorcycle
endpoint exposes it and the statistics summary flags it fleet-wide
(`hasConsumptionDrop`).

## Budgets and overspend

Budgets cover **maintenance spend only** (the `Cost` recorded on maintenance
records), per user. The statistics summary compares the current month/year spend
with the configured budgets and sets `isMonthlyOverBudget` / `isAnnualOverBudget`.
Overspend and the consumption-drop signal are surfaced **in the app only** —
there is no email/push notification for them (unlike maintenance reminders).

## Odometer

Logging a fuel-up advances the motorcycle km history in the same transaction
when its odometer is above the current reading, so km-based maintenance
reminders stay current. A backdated entry below the odometer does not move it and
never fails the operation (same rule as maintenance records).

## API

See [api.md](api.md) — `FuelUpsController` (`/api/fuel-ups`) plus
`PUT /api/users/me/budget` and the fuel/budget fields on
`GET /api/statistics/summary`.
