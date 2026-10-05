# Architecture Decision Records

One file per irreversible or costly decision: `NNN-title.md` with **Context**, **Options**,
**Decision** and **Consequences**. Written by the human, in a few paragraphs — not generated as a
report.

- [001-web-push.md](001-web-push.md) — Web Push for reminders (SwPush + Angular SW, VAPID, no custom SW).
- [002-disable-inline-critical-css.md](002-disable-inline-critical-css.md) — inline critical CSS off (CSP-compatible production build).
- [003-account-deletion.md](003-account-deletion.md) — self-service account deletion: anonymize the row, purge owned data.

Use an ADR when a choice is hard to reverse or future-you needs the *why* (storage, security
trade-offs, schema strategy, protocol, deployment topology). For everyday changes, update the
relevant [spec](../specs/) and the code instead.

The first worth recording here: why PostgreSQL, why `ITransactionManager` (unit-of-work over
per-repository `SaveChanges`), and why sliding refresh-token sessions.
