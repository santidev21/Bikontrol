---
name: security-review
description: Security audit checklist for Bikontrol. Use when reviewing authentication, JWT/refresh tokens, authorization, input validation, secrets, CORS, SQL/EF queries, XSS, PWA caching, dependencies, or any security-sensitive change.
---

# Security review (Bikontrol)

Read-only audit. Report findings with severity and `path:line` evidence. Never log or copy secret values — reference the variable name only.

## Secrets & config
- JWT key, SMTP credentials and connection strings live only in `.env` / `appsettings.Development.json` (both gitignored). Never in code, `appsettings.json`, Angular `environment.ts`, or git history.
- `.example` templates must contain placeholders, never real values.
- Flag any committed secret as Critical; recommend rotation, not just removal.
- The Angular bundle is public: anything in `bikontrol-web/src/environments/*` is visible to users — no secrets there, ever.

## Authentication & sessions
- Passwords hashed with per-user salt; never stored or logged in plaintext.
- JWT has a short expiry; refresh tokens are rotating/revocable (sliding sessions). Check `AuthService`, `refreshInterceptor`, and the API token handlers.
- `localStorage` token storage is XSS-exposed — acceptable here but it makes XSS prevention non-negotiable; report it as a known trade-off with a note, not a false Critical.
- Login/register/password-recovery/refresh are rate-limited (frontend already maps HTTP 429).
- Password recovery tokens are single-use and time-bound; responses don't reveal whether an email exists.
- Google OAuth: validate the id_token server-side (issuer, audience, expiry) — never trust the client payload.

## Authorization
- Every non-public endpoint has `[Authorize]` (or is intentionally anonymous — flag and confirm).
- **Ownership checks**: a user can only read/modify their own motorcycles, maintenance records, km history and profile. Missing ownership filter = Critical (IDOR).
- Admin-only operations (if any) require an explicit role/policy check.
- Soft deletes respected on reads; no endpoint can resurrect or hard-delete another user's data.

## Input validation
- Server-side validation on every input endpoint; never rely on Angular validation. Check `[Required]`/DataAnnotations + `ModelState` or FluentValidation.
- Validate ids, route params, enums and numeric ranges. Reject unknown/extra fields where relevant.
- CHECK constraints (km, intervals) exist in the DB — don't bypass them from the application.

## Data access
- EF Core parameterized LINQ only. Flag `FromSqlRaw`/`ExecuteSqlRaw` built by string interpolation or concatenation.
- No `SELECT *`-style over-fetching of sensitive columns; project to DTOs.
- Optimistic concurrency (`xmin`) preserved on updates that need it.

## Errors & logging
- Production must not leak stack traces, SQL, or internal messages. `HttpErrorService` should receive generic server errors.
- Logs must not contain tokens, passwords, emails-in-full or PII beyond what's needed.
- Failed auth attempts are logged (without credentials).

## API surface
- CORS is an explicit allow-list of known origins; no `AllowAnyOrigin` combined with credentials.
- HTTPS enforced in production; HSTS on.
- Security headers present (CSP, X-Content-Type-Options, X-Frame-Options/frame-ancestors, Referrer-Policy).
- Request body size limits and pagination to prevent abuse.
- No verbose `/health`/`/swagger` exposure in production.

## Frontend
- XSS: no `[innerHTML]`, `bypassSecurityTrustHtml/Url/ResourceUrl` on user-controlled data.
- Never build URLs/redirects from unvalidated query params (`open redirect`).
- SweetAlert2 content is not used with raw HTML from the server.
- No secrets/API keys baked into the bundle.
- Dependency versions pinned; run a vulnerabilities check when deps change.

## Dependencies
- AutoMapper stays pinned to `12.0.1` (v15+ needs a license and breaks the net8.0 JWT stack) — an upgrade is a security/business risk, flag it.
- Check `npm audit` / `dotnet list package --vulnerable` when dependencies change.

## Output format
For each finding, use:
```
[Severity: Critical|High|Medium|Low] Title
Where: path:line
Impact: what an attacker could do
Fix: concrete remediation
```
Order by severity. If a category is clean, say so explicitly — do not invent issues.
