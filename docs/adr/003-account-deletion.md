# ADR 003 — Self-service account deletion (anonymize + purge)

## Context

GDPR-style "right to erasure": a user must be able to delete their account and data without emailing
support. Bikontrol uses **soft deletes** on its core entities (`IsEnabled = false`) and keeps an
**append-only audit log** (`audit_logs`) that references the acting user by `UserId` (no FK).

Options considered:

1. **Hard-delete the user row and everything.** Simplest "erasure", but it destroys the audit trail
   (who did what) and any history the operator may need for abuse/incident investigation.
2. **Soft-delete the account (a disabled flag).** Nothing is really erased, the email stays taken, and
   personal data lingers — the opposite of what the flow promises.
3. **Anonymize the account row + hard-delete the owned data.** The user row survives as a tombstone so
   audit entries keep a stable owner reference, while all personal data is gone.

## Decision

Use **option 3**, implemented as `POST /api/users/me/delete` (`IAccountService.DeleteMyAccountAsync`):

- **Authorization**: the request must carry the confirmation word `ELIMINAR`; password accounts must
  also send the current password (verified with the same hasher as login). Google-only accounts have no
  password, so the authenticated session plus the typed word is the confirmation. Demo accounts are
  rejected (the demo tenant is shared).
- **Purge** (`IAccountRepository.PurgeUserDataAsync`, inside one transaction): attachments, maintenance
  records, user maintenances, km history, motorcycles, reminder logs, push subscriptions and refresh
  tokens of that user. Deletes run as bulk `ExecuteDeleteAsync` (immediate) and are intentionally **not**
  audited, so the audit trail is not bloated with the PII being erased.
- **Anonymize** (`User.Anonymize`): email → `deleted+{id}@bikontrol.local` (frees the original address
  for a future registration), name → "Cuenta eliminada", password hash → an unusable random value,
  tokens/lockout/reminder flags cleared. The change is audited like any other update (secrets redacted).
- **Sessions**: refresh tokens are deleted, so no session can be extended. The current access token
  (JWT) stays valid until it expires (`Jwt:ExpireMinutes`, default 15 min); the client logs out
  immediately after.

## Consequences

- Personal data is erased and the account is unusable, while the audit trail keeps a referential owner.
- The original email can register again — expected, and cheap to support because the row no longer
  claims it.
- The purge bypasses the audit interceptor and the soft-delete convention on purpose; it is always
  wrapped in a transaction, and a failure rolls back the whole deletion.
- A leaked access token could still call the API for up to the token TTL, operating on an anonymized
  account with no data. Accepted: rotating the password/tokens is what closes real sessions, and a
  future `TokenVersion`/`AnonymizedAt` claim check could remove even that window.
