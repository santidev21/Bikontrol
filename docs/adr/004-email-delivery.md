# ADR 004 — Email delivery: provider-agnostic SMTP, HTML + one-click unsubscribe, no outbox

## Context

Bikontrol sends two kinds of email: account mail (confirm address, reset password, "you already have
an account") and the daily reminder digest. Until now they were plain text over SMTP, with no way to
unsubscribe, which mailbox providers increasingly treat as spam signals (Gmail and Yahoo require SPF,
DKIM, DMARC and one-click unsubscribe for bulk mail).

Options considered:

1. **Provider SDK (Resend/Postmark/SES API).** Better delivery telemetry, but ties the code to one
   vendor and adds a dependency per provider.
2. **Keep SMTP, make the content and headers right.** All three providers expose SMTP, so switching is
   a configuration change (`Smtp__Host/Username/Password`).
3. **Add an email outbox table with a retrying worker.** Decouples requests from the SMTP server.

## Decision

Option 2. `IEmailSender` takes an `EmailMessage` (text + optional HTML + optional unsubscribe URL);
`SmtpEmailSender` builds a multipart message and adds `List-Unsubscribe` / `List-Unsubscribe-Post`
(RFC 2369 / RFC 8058) for the digest. All emails share one inline-styled template
(`EmailTemplate`), with every dynamic value HTML-encoded.

The digest's unsubscribe link is stateless: `base64url(userId).base64url(HMAC-SHA256)`, keyed with a
purpose-specific key derived from `Jwt:Key`. It never expires, needs no login, and
`POST /api/reminders/email/unsubscribe` is idempotent and answers the same for unknown users. The
link in the email body opens a frontend page that only acts on a button press, because link
scanners open URLs.

**No outbox.** The digest already behaves like a queue: undelivered reminders stay `Pending`, each
send is persisted before the next, and the next daily run retries. The account emails carry live
reset/confirmation tokens; persisting them in an outbox table would keep secrets in the database that
the rest of the design deliberately stores only as hashes. The SMTP call already retries transient
failures with a timeout (`TransientRetry`).

## Consequences

- Switching provider is configuration only. Delivery tracking (bounces, complaints) is whatever the
  provider dashboard offers; the app does not ingest webhooks.
- Deliverability depends on DNS: SPF, DKIM and DMARC for the `From` domain. `scripts/check-email-dns.sh`
  verifies them; the checklist is in [DEPLOYMENT](../DEPLOYMENT.md#email-deliverability).
- Rotating `Jwt:Key` invalidates old unsubscribe links (they would answer 400). Acceptable: the user can
  still switch reminders off in their profile.
- If volume or SMTP latency ever makes request-time sending a problem, an outbox for *non-secret* mail
  (the digest) is the natural next step; account mail would need encrypted payloads first.
