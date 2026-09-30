# Architecture Decision Records

One file per irreversible or costly decision: `NNN-title.md` with **Context**, **Options**,
**Decision** and **Consequences**. Written by the human, in a few paragraphs — not generated as a
report.

Use an ADR when a choice is hard to reverse or future-you needs the *why* (storage, security
trade-offs, schema strategy, protocol, deployment topology). For everyday changes, update the
relevant [spec](../specs/) and the code instead.

The first worth recording here: why PostgreSQL, why `ITransactionManager` (unit-of-work over
per-repository `SaveChanges`), and why sliding refresh-token sessions.
