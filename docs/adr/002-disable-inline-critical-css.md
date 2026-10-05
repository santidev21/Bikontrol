# ADR 002 — Disable Angular critical-CSS inlining (CSP-safe build)

## Context

Production serves the Angular app with a strict CSP (no `script-src 'unsafe-inline'`). Angular's
production build enables **critical CSS inlining** by default (`optimization.styles.inlineCritical`).
That transform extracts the above-the-fold CSS into a `<style>` block and defers the full stylesheet
with `media="print"`, re-enabling it at runtime through an **inline** handler:

```html
<link rel="stylesheet" href="styles-XXXX.css" media="print" onload="this.media='all'">
```

Inline event handlers/scripts require `'unsafe-inline'` (or a hash/nonce) in the CSP. Our CSP blocks
them, so the handler never ran: the full stylesheet stayed in print media and the whole app rendered
**without styles**, on every device (not a caching issue).

Options considered:

1. **Add `'unsafe-inline'` to `script-src`.** Weakens the CSP for every page; rejected.
2. **CSP hashes/nonces for the injected inline.** The inline content changes on every build, so it
   means generating and shipping a per-build hash into the gateway CSP — brittle, and the CSP lives
   in a separate repo. Rejected.
3. **Disable `inlineCritical`.** The build emits a normal `<link rel="stylesheet">` with no inline
   handler or inline critical block; nothing for the CSP to block.

## Decision

Use **option 3**: set `optimization.styles.inlineCritical=false` in the Angular production config.

- Trade-off: a small first-paint cost (the full CSS is no longer inlined into the HTML). Accepted —
  the stylesheet is ~18 KB and the CSP/no-inline-handler invariant is worth more.
- The gateway CSP stays strict; no `'unsafe-inline'` is added.
- `ngsw-config.json` also excludes `/ready`, `/health` and `/api/**` from service-worker navigation
  so those routes are never served the cached SPA shell.

## Consequences

- Styles render on first load on every device, with the strict CSP intact.
- Anyone changing the production build config must **not** re-enable `inlineCritical` while the CSP
  forbids inline scripts. A build with inline critical CSS silently regresses styling.
