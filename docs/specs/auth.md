# Auth

- Authentication uses JWT and password hashing.
- Login, register and Google login store the JWT in `localStorage` along with a `refreshToken`.
- `auth.interceptor.ts` adds `Authorization: Bearer <token>` automatically.
- `refresh.interceptor.ts` transparently renews the session: on a `401` it calls `POST /api/auth/refresh` once and retries the original request. Concurrent `401`s share a single in-flight refresh (`shareReplay`); auth URLs (`/auth/`) never trigger a refresh. If the refresh fails it clears the session and redirects to `/login`.
- Role claim: JWTs carry `role` (`User`/`Demo`). The API sets `MapInboundClaims = false` with `RoleClaimType = "role"` so the claim is not remapped, and `CurrentUserService` accepts both `role` and `ClaimTypes.Role` (older mapped tokens). Demo write guards depend on this — do not change claim handling without updating the service and its tests.
- Route guards: `authGuard` protects the whole `/dashboard` branch (redirects to `/login` when unauthenticated), `guestGuard` keeps authenticated users out of `/login` and `/register`.
- UI errors should be surfaced to the user, not logged with `console.error`.
- Per-clone signing secret: generate with `openssl rand -base64 48` and put it in `Jwt:Key` inside `Bikontrol/Bikontrol.API/appsettings.Development.json` (never committed); production uses `Jwt__Key` env.
- Password hashing with a per-user salt.

## Endpoints (`api/auth`)

| Method | Route | Notes |
|---|---|---|
| `POST` | `/register` | Creates a user. With email confirmation required (default) it returns **no session** and emails a confirmation link; otherwise it returns access + refresh tokens. |
| `POST` | `/login` | Email + password, returns access + refresh token. Returns `429` while the account is locked out. |
| `POST` | `/google` | Body `{ idToken }`. Validates a Google ID token (`Google__ClientId`), creates the user if missing, returns Bikontrol tokens. |
| `POST` | `/refresh` | Body `{ refreshToken }`. Single-use rotation: concurrent replays conflict and return `401` (expired session). |
| `POST` | `/forgot-password` | Body `{ email }`. Emails a reset link (always `200` to avoid leaking account existence). |
| `POST` | `/reset-password` | Body `{ email, token, newPassword }`. Validates the hashed, time-limited token and updates the password. |
| `POST` | `/confirm-email` | Body `{ email, token }`. Validates the hashed, time-limited confirmation token and marks the email confirmed. Idempotent. |
| `POST` | `/resend-confirmation` | Body `{ email }`. Re-sends the confirmation link if the account exists and is unconfirmed (always `200` to avoid leaking account existence). |
| `POST` | `/demo` | Anonymous demo login. **Opt-in**: returns `404` unless `Demo:Enabled=true`. Returns `403` when the configured demo email belongs to a non-demo account (never hands out real accounts). |

## Demo mode

- The public demo tenant is **opt-in** via `Demo:Enabled` (env `Demo__Enabled`). It defaults to `false` everywhere, including production.
- When disabled: `POST /api/auth/demo` returns `404` and `DemoUserSeeder` runs no queries — the demo user and its content are never created.
- When enabled: the seeder creates `demo@bikontrol.com` (`Role=Demo`) plus sample motorcycles/maintenances; the account is read-only (writes return `403`).
- Development enables it through `appsettings.Development.json`; the Angular build mirrors the flag through `environment.demoEnabled`, which hides the "Probar demo" button when off. Keep the API flag and the frontend flag in sync when enabling a public demo.

## Email confirmation

- Opt-out via `EmailConfirmation:Required` (env `EmailConfirmation__Required`), default **true**. It fails secure: an absent value means *required*.
- With verification on, registration creates the account **without a session**, stores a 48-byte token (SHA-256 hash + 24h expiry) and emails a link to `Frontend__BaseUrl/confirm-email?token=...&email=...`. `POST /api/auth/login` returns `403` until `users.EmailConfirmedAt` is set.
- Google accounts are auto-confirmed (Google already verified the email); demo accounts are confirmed when the demo session is created.
- Existing accounts are grandfathered by the migration (`EmailConfirmedAt = CreatedAt`), so nobody is locked out after deploy.
- With verification off, registration marks the account confirmed immediately and returns a session (previous behavior).
- The frontend mirrors this: the register screen shows a "check your inbox" state, `/confirm-email` completes the flow, and login offers to resend the link on `403`.

## Account lockout (anti brute force)

- After `Lockout:MaxFailedAttempts` (default 5) consecutive failed logins the account is locked for `Lockout:Minutes` (default 15). The password hasher is skipped while locked, and login returns `429`.
- A successful login clears `AccessFailedCount`; a successful password reset also unlocks the account.
- Opt-out via `Lockout:Enabled` (default true).
- The demo account is unlocked when its session is issued, so visitors are never blocked by a lockout.

## Sessions (refresh tokens)

- Access token lifetime: `Jwt__ExpireMinutes` (default 15 min). Refresh token lifetime: `Jwt__RefreshExpireDays` (default 30 days).
- Refresh tokens are stored in the `refresh_tokens` table (SHA-256 hash only) and are revoked/rotated on every use. `RevokedAt` is a concurrency token, so two simultaneous refreshes with the same token cannot both mint replacements.
- Changing the password (`POST /api/users/me/password`) or resetting it (`POST /api/auth/reset-password`) revokes **all** active refresh tokens for the user, so any stolen session dies with the password change.
- Google-only accounts get a random password hash; they can adopt a password through the recovery flow.

## Password recovery

- Reset tokens are random 48-byte values, stored as SHA-256 hashes in `users.ResetPasswordTokenHash` and expire after 2 hours (`users.ResetPasswordTokenExpires`).
- Emails are sent through the SMTP settings in `.env` (`Smtp__Host`, `Smtp__Port`, `Smtp__Username`, `Smtp__Password`, `Smtp__FromEmail`, `Smtp__FromName`, `Smtp__EnableSsl`). If SMTP is not configured, the reset link is logged to the console **only in Development**; in other environments the body is never logged (it contains a live token) and only a generic warning is emitted.
- The reset link points at `Frontend__BaseUrl` → `/reset-password?token=...&email=...`.

## Google OAuth

- Uses the Google Identity Services **ID-token flow** (`Sign in with Google`); the Client ID is public and also compiled into the Angular app via `environment.googleClientId`. No Client Secret is required.
- Configure `Google__ClientId` in `.env` / `appsettings`. In the Google Cloud Console add the app origins to **Authorized JavaScript origins** (`https://bikontrol.santidev21.tech`, `http://localhost:4200`, `http://localhost:4201`).