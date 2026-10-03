# ADR 001 — Web Push for maintenance reminders

## Context

The reminder engine (daily job + digest email) is delivered. The plan also calls for **Web Push**, so a
rider with the PWA installed gets a notification even when the app is closed. Bikontrol serves its
frontend through the **Angular service worker** (`ngsw`), which owns the root scope.

Options considered:

1. **Custom service worker** handling `push`/`notificationclick` registered alongside `ngsw`. Two
   service workers cannot both control the same scope, so this would mean giving up `ngsw` (offline
   app-shell, asset caching, update flow) — a large regression for the PWA.
2. **`SwPush` + the Angular service worker.** Angular's `ngsw` supports push natively: it listens for
   `PushEvent`, turns the payload's `notification` object into a `Notification`, and honours
   `data.onActionClick` to open the app. `SwPush` exposes `requestSubscription`, `subscription` and
   `unsubscribe`.
3. **A third-party push provider** (OneSignal/FCM): external dependency, user data leaves the
   perimeter, and it fights the "no over-engineering" guardrail.

## Decision

Use **option 2**: `SwPush` with the existing Angular service worker.

- The **frontend** subscribes with `SwPush.requestSubscription({ serverPublicKey })` and posts the
  subscription (`endpoint` + `keys.p256dh` + `keys.auth`) to the API; unsubscribe removes it.
- The **backend** stores subscriptions in `push_subscriptions` (one row per endpoint, upsert on
  re-subscribe), signs with **VAPID** (`WebPush:PublicKey`/`PrivateKey`) via the `WebPush` library,
  and sends the Angular-shaped payload
  (`{ notification: { title, body, data: { onActionClick: { default: { operation: "openWindow", url } } } } }`).
- Push is **inert without VAPID keys** (a no-op), and gone subscriptions (404/410) are deleted.
- Keys are **operator-provided** (`WebPush__PublicKey`/`WebPush__PrivateKey` in `.env`, never
  committed); generating and rotating them is documented in `docs/DEPLOYMENT.md`.

## Consequences

- No custom service worker, so the PWA's offline/update behaviour is untouched.
- Push and email share the same due detection and dedupe, so a user never gets contradictory or
  duplicate nagging; a reminder is delivered by email, push, or both, and marked delivered per channel.
- Rotating VAPID keys invalidates existing subscriptions (browsers must re-subscribe), so it is a
  deliberate, documented operation, not routine.
- Keeping this reversible: the whole engine (including push) is behind `Reminders:*` flags; disabling
  them returns the app to a plain per-user dashboard with no reminders.
