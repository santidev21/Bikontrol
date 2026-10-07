#!/usr/bin/env node
/**
 * Bikontrol — seed a real motorcycle with a realistic maintenance history.
 *
 * It talks to the public API (login + the same endpoints the app uses), so it
 * runs against production without DB access and respects every business rule
 * (ownership, interval validation, "records must be chronological", etc.).
 *
 * WHAT IT DOES
 *   1. Reads the motorcycle's current km and model year from the API.
 *   2. Creates one user maintenance per item of the catalog below (skipping the
 *      ones that already exist by name).
 *   3. Registers past records for each item, distributing the km/date evenly
 *      between the start of use and today.
 *
 * SAFETY
 *   Dry-run by default: it only prints what it would create. Pass --apply to
 *   actually write. Re-running is idempotent (existing maintenances are reused
 *   and existing records are skipped).
 *
 * USAGE
 *   # 1. Get a token. Easiest: copy localStorage["refreshToken"] (lasts ~30 days).
 *   export BIKONTROL_REFRESH_TOKEN='...'
 *   #    Or the short-lived access token localStorage["token"] (lasts ~15 min):
 *   # export BIKONTROL_TOKEN='...'
 *
 *   # 2. Preview (auto-detects your motorcycle).
 *   node scripts/seed-maintenances.mjs
 *
 *   # 3. Apply.
 *   node scripts/seed-maintenances.mjs --apply
 *
 * Local dev note: point API_URL at http://localhost:5202/api (plain HTTP) so
 * Node does not reject Kestrel's self-signed certificate.
 *
 * ENV
 *   API_URL                 API base URL         (default: production)
 *   BIKONTROL_REFRESH_TOKEN refresh token        (preferred, lasts ~30 days)
 *   BIKONTROL_TOKEN         access token JWT     (short-lived fallback)
 *   BIKONTROL_EMAIL         account email        (fallback when no token)
 *   BIKONTROL_PASSWORD      account password     (fallback when no token)
 *   MOTORCYCLE_ID           motorcycle id        (optional: auto-detected; set it
 *                                               only when the account has several)
 *   USER_ID                 user id              (optional, only for the log)
 *   RIDE_START_DATE         YYYY-MM-DD start of use (default: Jan 1 of model year)
 *   CURRENT_KM              override the ceiling km (default: detected from API)
 */

import { readFileSync } from 'node:fs';

/** True once a refresh token was used (which rotates and invalidates it). */
let usedRefreshToken = false;

const CONFIG = {
  apiUrl: (process.env.API_URL || 'https://bikontrol.santidev21.tech/api').replace(/\/$/, ''),
  token: (process.env.BIKONTROL_TOKEN || '').trim().replace(/^["']/, '').replace(/["']$/, ''),
  refreshToken: (process.env.BIKONTROL_REFRESH_TOKEN || '')
    .trim()
    .replace(/^["']/, '')
    .replace(/["']$/, ''),
  email: process.env.BIKONTROL_EMAIL || '',
  password: process.env.BIKONTROL_PASSWORD || '',
  motorcycleId: process.env.MOTORCYCLE_ID || '',
  userId: process.env.USER_ID || '',
  rideStartDate: process.env.RIDE_START_DATE || '',
  currentKmOverride: Number(process.env.CURRENT_KM || 0),
  apply: process.argv.includes('--apply'),
  debug: process.argv.includes('--debug'),
};

/**
 * Catalog from the owner's manual. `trackingType` is the metric the app uses
 * for the countdown; `timeIntervalWeeks` is stored alongside so the "or every
 * X" rule stays visible even when the item is tracked by km.
 *
 * The app tracks one metric per maintenance ("Km" or "Time"), so an item like
 * "oil every 4.000 km or 12 months, whichever comes first" is tracked by km
 * here; the 12-month note is kept in the description.
 */
const CATALOG = [
  {
    name: 'Cambio de aceite del motor',
    description: 'Cada 4.000 km o 12 meses, lo que ocurra primero.',
    trackingType: 'Km',
    kmInterval: 4000,
    timeIntervalWeeks: 52,
  },
  {
    name: 'Cambio de filtro de aceite',
    description: 'Cada 4.000 km o 12 meses, lo que ocurra primero.',
    trackingType: 'Km',
    kmInterval: 4000,
    timeIntervalWeeks: 52,
  },
  {
    name: 'Reemplazo de filtro de aire',
    description: 'Reemplazo cada 8.000 km.',
    trackingType: 'Km',
    kmInterval: 8000,
  },
  {
    name: 'Cambio de filtro de gasolina',
    description: 'Cambio cada 20.000 km.',
    trackingType: 'Km',
    kmInterval: 20000,
  },
  {
    name: 'Limpieza de cadena',
    description: 'Cada 500–1.000 km, o al contaminarse / tras lluvia o barro.',
    trackingType: 'Km',
    kmInterval: 1000,
  },
  {
    name: 'Ajuste/tensión de cadena',
    description: 'Revisar y ajustar cada 1.000 km.',
    trackingType: 'Km',
    kmInterval: 1000,
  },
  {
    name: 'Cambio de kit de arrastre',
    description: 'Reemplazo programado cada 20.000 km.',
    trackingType: 'Km',
    kmInterval: 20000,
  },
  {
    name: 'Pastillas de freno delanteras',
    description: 'Cambio cada 6.000 km.',
    trackingType: 'Km',
    kmInterval: 6000,
  },
  {
    name: 'Pastillas de freno traseras',
    description: 'Cambio cada 6.000 km.',
    trackingType: 'Km',
    kmInterval: 6000,
  },
  {
    name: 'Disco de freno delantero',
    description: 'Cambio cada 30.000 km.',
    trackingType: 'Km',
    kmInterval: 30000,
  },
  {
    name: 'Disco de freno trasero',
    description: 'Cambio cada 30.000 km.',
    trackingType: 'Km',
    kmInterval: 30000,
  },
  {
    name: 'Cambio de líquido refrigerante',
    description: 'Cada 2 años (104 semanas).',
    trackingType: 'Time',
    timeIntervalWeeks: 104,
  },
];

// ---------------------------------------------------------------------------
// HTTP helpers
// ---------------------------------------------------------------------------

async function api(method, path, body) {
  const headers = { Accept: 'application/json' };
  if (CONFIG.token) headers.Authorization = `Bearer ${CONFIG.token}`;
  if (body !== undefined) headers['Content-Type'] = 'application/json';

  const res = await fetch(`${CONFIG.apiUrl}${path}`, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });

  const text = await res.text();
  const data = text ? safeJson(text) : null;

  if (!res.ok) {
    const detail = sanitizeForLog(
      (data && (data.message || data.title || data.error)) || text || res.statusText,
    );
    let hint = '';
    if (res.status === 401) {
      hint = path.includes('/auth/refresh')
        ? ' (refresh token no longer valid: refresh tokens rotate, so the copy is ' +
          'revoked as soon as the open app refreshes. Use BIKONTROL_EMAIL + ' +
          'BIKONTROL_PASSWORD, or copy a fresh value and run immediately.)'
        : ' (token rejected: it may be expired — access tokens last ~15 min —, the ' +
          'refreshToken was copied instead of token, or the value is "null"/empty. ' +
          'Log in again and copy localStorage["token"] right before running.)';
    }
    const error = new Error(`${method} ${path} -> ${res.status}: ${detail}${hint}`);
    error.status = res.status;
    throw error;
  }
  return data;
}

function safeJson(text) {
  try {
    return JSON.parse(text);
  } catch {
    return null;
  }
}

/**
 * Neutralises server-controlled text before it reaches the terminal. Strips
 * control characters (including CR/LF, which could forge log lines) and caps
 * the length, so an error stays readable without risking log injection.
 */
function sanitizeForLog(value) {
  return String(value ?? '')
    .replace(/[\u0000-\u001f\u007f-\u009f]+/g, ' ')
    .slice(0, 500);
}

async function authenticate() {
  // A value that is not a JWT but was put in BIKONTROL_TOKEN is really a refresh
  // token (48 random bytes -> 64 base64 chars). Normalise it so it can never
  // shadow a refresh token set elsewhere.
  if (CONFIG.token && !looksLikeJwt(CONFIG.token)) {
    if (!CONFIG.refreshToken) CONFIG.refreshToken = CONFIG.token;
    CONFIG.token = '';
    log('Note: BIKONTROL_TOKEN was not a JWT; using it as a refresh token.');
  }

  if (CONFIG.token) {
    inspectToken(CONFIG.token);
    return;
  }
  if (CONFIG.refreshToken) {
    try {
      const session = await api('POST', '/auth/refresh', { refreshToken: CONFIG.refreshToken });
      CONFIG.token = session.token;
      usedRefreshToken = true;
      log(`Session refreshed (token valid ~${Math.round(session.expiresIn / 60)} min).`);
      return;
    } catch (error) {
      // Refresh tokens rotate: the copy may have been revoked by the open app.
      log(`Refresh failed (${error.message.split(':')[0]}). ` +
          (CONFIG.email && CONFIG.password ? 'Falling back to email/password login.' : 'No email/password to fall back to.'));
      if (!CONFIG.email || !CONFIG.password) throw error;
    }
  }
  if (!CONFIG.email || !CONFIG.password) {
    throw new Error(
      'Missing credentials: set BIKONTROL_REFRESH_TOKEN (or BIKONTROL_TOKEN), ' +
        'or BIKONTROL_EMAIL + BIKONTROL_PASSWORD.',
    );
  }
  const session = await api('POST', '/auth/login', {
    email: CONFIG.email,
    password: CONFIG.password,
  });
  CONFIG.token = session.token;
  log(`Authenticated (role ${session.role}).`);
}

function looksLikeJwt(value) {
  return value.split('.').length === 3 && value.startsWith('eyJ');
}

/**
 * Decodes the access token to warn about the usual 401 causes. It never prints
 * the token or any of its claims — those come from the environment and are
 * treated as sensitive.
 */
function inspectToken(token) {
  const parts = token.split('.');
  if (parts.length !== 3 || !token.startsWith('eyJ')) {
    log(
      'Warning: BIKONTROL_TOKEN does not look like a JWT. Make sure you copied ' +
        'localStorage["token"] (not "null", not the refreshToken).',
    );
    return;
  }
  try {
    const payload = JSON.parse(Buffer.from(parts[1], 'base64url').toString('utf8'));
    if (payload.role === undefined) {
      log('Warning: this looks like the refreshToken, not the access token.');
      return;
    }
    if (payload.exp && payload.exp - Date.now() / 1000 <= 0) {
      log('Warning: the access token is expired; log in again and copy a fresh one.');
    }
  } catch {
    log('Warning: could not decode the token payload.');
  }
}

// ---------------------------------------------------------------------------
// Domain helpers
// ---------------------------------------------------------------------------

const DAY_MS = 24 * 60 * 60 * 1000;

function parseDate(value) {
  const date = new Date(`${value}T00:00:00Z`);
  if (Number.isNaN(date.getTime())) throw new Error(`Invalid date: ${value}`);
  return date;
}

/**
 * Best-effort start of use: an explicit RIDE_START_DATE, otherwise January 1st
 * of the model year (never in the future, never before 1950).
 */
function resolveRideStart(year) {
  if (CONFIG.rideStartDate) return parseDate(CONFIG.rideStartDate);

  const now = new Date();
  let start = new Date(Date.UTC(Math.min(year, now.getUTCFullYear()), 0, 1));
  if (start > now) start = new Date(Date.UTC(now.getUTCFullYear(), 0, 1));
  return start;
}

/** Linear km -> date mapping between the start of use and today. */
function makeDateForKm(startDate, ceilingKm) {
  const now = new Date();
  const span = Math.max(now.getTime() - startDate.getTime(), 0);
  return (km) => {
    const ratio = ceilingKm > 0 ? Math.min(1, Math.max(0, km / ceilingKm)) : 0;
    return new Date(startDate.getTime() + ratio * span);
  };
}

/** Km multiples to record for a km-tracked item: I, 2I, 3I... up to the ceiling. */
function kmTargets(interval, ceilingKm) {
  const list = [];
  if (interval <= 0) return list;
  for (let km = interval; km <= ceilingKm; km += interval) list.push(km);
  return list;
}

/** Dates every `weeks` from the start of use up to today. */
function timeTargets(weeks, startDate) {
  const list = [];
  const stepMs = weeks * 7 * DAY_MS;
  if (stepMs <= 0) return list;
  const now = new Date();
  for (let t = startDate.getTime() + stepMs; t <= now.getTime(); t += stepMs) {
    list.push(new Date(t));
  }
  return list;
}

const isoDate = (d) => d.toISOString().slice(0, 10);

// ---------------------------------------------------------------------------
// Plan
// ---------------------------------------------------------------------------

async function buildPlan(motorcycleId) {
  const motorcycle = await api('GET', `/motorcycles/${motorcycleId}`);
  const detectedKm = motorcycle.km ?? (await api('GET', `/motorcycles/${motorcycleId}/km/current`)).km;

  // Never target km above the real odometer: a record past it would push the
  // odometer forward. CURRENT_KM can only shrink the history, not extend it.
  const ceilingKm =
    CONFIG.currentKmOverride > 0 ? Math.min(CONFIG.currentKmOverride, detectedKm) : detectedKm;
  if (CONFIG.currentKmOverride > 0 && CONFIG.currentKmOverride > detectedKm) {
    log('Note: CURRENT_KM is above the odometer; using the real odometer reading.');
  }

  const startDate = resolveRideStart(motorcycle.year);
  const dateForKm = makeDateForKm(startDate, ceilingKm);

  const existing = await api('GET', `/maintenances/mine/motorcycle/${motorcycleId}`);
  const byName = new Map(existing.map((m) => [m.name.trim().toLowerCase(), m]));

  const existingRecords = await api('GET', `/maintenances/motorcycle/${motorcycleId}/records`);
  const recordsByMaintenance = new Map();
  for (const r of existingRecords) {
    const list = recordsByMaintenance.get(r.userMaintenanceId) ?? [];
    list.push(r);
    recordsByMaintenance.set(r.userMaintenanceId, list);
  }

  const items = [];
  for (const def of CATALOG) {
    const found = byName.get(def.name.toLowerCase());
    const action = found ? 'reuse' : 'create';

    // Existing records cap the ones we still may add.
    const prior = found ? recordsByMaintenance.get(found.id) ?? [] : [];
    const priorKms = new Set(prior.map((r) => r.performedKm).filter((k) => k != null));
    const priorDates = new Set(prior.map((r) => r.performedAt.slice(0, 10)));
    const maxPriorKm = priorKms.size ? Math.max(...priorKms) : 0;

    const records = [];
    if (def.trackingType === 'Km') {
      for (const km of kmTargets(def.kmInterval, ceilingKm)) {
        if (km <= maxPriorKm || priorKms.has(km)) continue;
        // Full ISO instant (UTC): the API/Postgres reject a Kind=Unspecified date.
        records.push({ performedKm: km, performedAt: dateForKm(km).toISOString() });
      }
    } else {
      for (const date of timeTargets(def.timeIntervalWeeks, startDate)) {
        if (priorDates.has(date.toISOString().slice(0, 10))) continue;
        records.push({ performedKm: null, performedAt: date.toISOString() });
      }
    }

    items.push({ def, action, maintenance: found ?? null, records });
  }

  return { motorcycle, detectedKm, ceilingKm, startDate, items };
}

function printPlan(plan) {
  log('');
  log(`Motorcycle : ${plan.motorcycle.name} ${plan.motorcycle.brand} (${plan.motorcycle.year}) ` +
      `"${plan.motorcycle.nickname}" — ${plan.motorcycle.id}`);
  log(`Odometer   : detected ${plan.detectedKm} km, ceiling used ${plan.ceilingKm} km`);
  log(`Start of use: ${isoDate(plan.startDate)} (assumed)`);
  log('');

  const rows = plan.items.map((i) => ({
    item: i.def.name,
    tracking: i.def.trackingType === 'Km' ? `${i.def.kmInterval} km` : `${i.def.timeIntervalWeeks} w`,
    maintenance: i.action,
    newRecords: i.records.length,
  }));
  console.table(rows);

  const total = plan.items.reduce((n, i) => n + i.records.length, 0);
  const creates = plan.items.filter((i) => i.action === 'create').length;
  log(`${creates} maintenance(s) to create, ${total} record(s) to register.`);
  if (!CONFIG.apply) log('DRY RUN — nothing was written. Re-run with --apply to persist.');
}

// ---------------------------------------------------------------------------
// Apply
// ---------------------------------------------------------------------------

async function applyPlan(plan) {
  let createdItems = 0;
  let createdRecords = 0;
  let skippedRecords = 0;

  for (const item of plan.items) {
    let maintenanceId = item.maintenance?.id;

    if (!maintenanceId) {
      const dto = {
        motorcycleId: CONFIG.motorcycleId,
        name: item.def.name,
        description: item.def.description,
        kmInterval: item.def.kmInterval ?? 0,
        timeIntervalWeeks: item.def.timeIntervalWeeks ?? 0,
        trackingType: item.def.trackingType,
      };
      const created = await api('POST', '/maintenances/mine', dto);
      maintenanceId = created.id;
      createdItems++;
      log(`  + maintenance: ${item.def.name}`);
    } else {
      log(`  = maintenance exists: ${item.def.name}`);
    }

    // Records must be sent oldest -> newest (the API enforces chronological km).
    const ordered = [...item.records].sort((a, b) =>
      a.performedKm != null && b.performedKm != null
        ? a.performedKm - b.performedKm
        : a.performedAt.localeCompare(b.performedAt),
    );

    for (const record of ordered) {
      try {
        await api('POST', '/maintenances/records', {
          motorcycleId: CONFIG.motorcycleId,
          userMaintenanceId: maintenanceId,
          performedAt: record.performedAt,
          performedKm: record.performedKm,
          cost: null,
        });
        createdRecords++;
        const when = record.performedAt.slice(0, 10);
        log(`      · ${when}${record.performedKm != null ? ` @ ${record.performedKm} km` : ''}`);
      } catch (error) {
        skippedRecords++;
        log(`      ! skipped ${record.performedAt.slice(0, 10)}: ${error.message}`);
      }
    }
  }

  log('');
  log(`Done: ${createdItems} maintenance(s) created, ${createdRecords} record(s) registered` +
      (skippedRecords ? `, ${skippedRecords} skipped.` : '.'));
}

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

function log(message) {
  // Every line goes through sanitizeForLog: server-controlled text can contain
  // control characters that would otherwise forge extra log lines.
  console.log(sanitizeForLog(message));
}

async function main() {
  if (process.argv.includes('--help') || process.argv.includes('-h')) {
    console.log(readFileSync(new URL(import.meta.url), 'utf8').split('*/')[0]);
    return;
  }

  // Never print the configured values themselves (they come from the
  // environment and are treated as sensitive): only which source was used.
  const apiSource = process.env.API_URL ? 'API_URL' : 'default (production)';
  log(`API: ${apiSource}${CONFIG.apply ? '' : '  (dry run)'}`);
  if (CONFIG.debug) {
    const authMethod = CONFIG.token
      ? 'access token'
      : CONFIG.refreshToken
        ? 'refresh token'
        : CONFIG.email
          ? 'email/password'
          : '(none)';
    log(`debug: auth method = ${authMethod}`);
  }
  await authenticate();

  if (!CONFIG.motorcycleId) CONFIG.motorcycleId = await resolveMotorcycleId();

  const plan = await buildPlan(CONFIG.motorcycleId);
  printPlan(plan);

  if (!CONFIG.apply) {
    if (usedRefreshToken) {
      log('Note: this dry run rotated your refresh token. Copy a fresh one before --apply.');
    }
    return;
  }
  await applyPlan(plan);
}

/**
 * When MOTORCYCLE_ID is not set, look it up in the API. A single motorcycle is
 * used automatically; with several, the list is printed so the id can be picked.
 */
async function resolveMotorcycleId() {
  const motorcycles = await api('GET', '/motorcycles/mine');
  if (!motorcycles.length) throw new Error('This account has no motorcycles to seed.');

  if (motorcycles.length === 1) {
    const only = motorcycles[0];
    log(`Motorcycle auto-detected: ${only.name} ${only.brand} (${only.year}) — ${only.id}`);
    return only.id;
  }

  log('Multiple motorcycles found — set MOTORCYCLE_ID to one of these:');
  for (const m of motorcycles) {
    log(`  ${m.id}  ${m.name} ${m.brand} (${m.year}) "${m.nickname}" — ${m.km} km`);
  }
  throw new Error('Ambiguous motorcycle: set MOTORCYCLE_ID and run again.');
}

main().catch((error) => {
  console.error(`\nError: ${sanitizeForLog(error?.message ?? error)}`);
  process.exitCode = 1;
});
