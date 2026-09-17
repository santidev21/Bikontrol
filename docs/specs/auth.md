# Auth

- Authentication uses JWT and password hashing.
- Login, register and Google login store the JWT in `localStorage` along with a `refreshToken`.
- `auth.interceptor.ts` adds `Authorization: Bearer <token>` automatically.
- `refresh.interceptor.ts` transparently renews the session: on a `401` it calls `POST /api/auth/refresh` once and retries the original request. If the refresh fails it clears the session and redirects to `/login`.
- Route guards: `authGuard` protects the whole `/dashboard` branch (redirects to `/login` when unauthenticated), `guestGuard` keeps authenticated users out of `/login` and `/register`.
- UI errors should be surfaced to the user, not logged with `console.error`.
- Per-clone signing secret: generate with `openssl rand -base64 48` and put it in `Jwt:Key` inside `Bikontrol/Bikontrol.API/appsettings.Development.json` (never committed); production uses `Jwt__Key` env.
- Password hashing with a per-user salt.

## Endpoints (`api/auth`)

| Method | Route | Notes |
|---|---|---|
| `POST` | `/register` | Creates a user, returns access + refresh token. |
| `POST` | `/login` | Email + password, returns access + refresh token. |
| `POST` | `/google` | Body `{ idToken }`. Validates a Google ID token (`Google__ClientId`), creates the user if missing, returns Bikontrol tokens. |
| `POST` | `/refresh` | Body `{ refreshToken }`. Rotates the refresh token and returns a new pair. |
| `POST` | `/forgot-password` | Body `{ email }`. Emails a reset link (always `200` to avoid leaking account existence). |
| `POST` | `/reset-password` | Body `{ email, token, newPassword }`. Validates the hashed, time-limited token and updates the password. |

## Sessions (refresh tokens)

- Access token lifetime: `Jwt__ExpireMinutes` (default 15 min). Refresh token lifetime: `Jwt__RefreshExpireDays` (default 30 days).
- Refresh tokens are stored in the `refresh_tokens` table (SHA-256 hash only) and are revoked/rotated on every use.
- Changing the password (`PUT /api/users/me/password`) or resetting it (`POST /api/auth/reset-password`) revokes **all** active refresh tokens for the user, so any stolen session dies with the password change.
- Google-only accounts get a random password hash; they can adopt a password through the recovery flow.

## Password recovery

- Reset tokens are random 48-byte values, stored as SHA-256 hashes in `users.ResetPasswordTokenHash` and expire after 2 hours (`users.ResetPasswordTokenExpires`).
- Emails are sent through the SMTP settings in `.env` (`Smtp__Host`, `Smtp__Port`, `Smtp__Username`, `Smtp__Password`, `Smtp__FromEmail`, `Smtp__FromName`, `Smtp__EnableSsl`). If SMTP is not configured, the reset link is logged to the console **only in Development**; in other environments the body is never logged (it contains a live token) and only a generic warning is emitted.
- The reset link points at `Frontend__BaseUrl` → `/reset-password?token=...&email=...`.

## Google OAuth

- Uses the Google Identity Services **ID-token flow** (`Sign in with Google`); the Client ID is public and also compiled into the Angular app via `environment.googleClientId`. No Client Secret is required.
- Configure `Google__ClientId` in `.env` / `appsettings`. In the Google Cloud Console add the app origins to **Authorized JavaScript origins** (`https://bikontrol.santidev21.tech`, `http://localhost:4200`, `http://localhost:4201`).