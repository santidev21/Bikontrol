# Loading feedback

Every request that can take time gives visible feedback. The app combines one
global indicator with per-surface states so nothing ever looks blank or frozen.

## Backbone (automatic)

- **`LoadingService`** (`shared/services/loading.service.ts`) — signal counter of
  in-flight requests (`begin()` / `end()`, `pendingCount`, `isLoading`). A request
  only raises the overlay after a short delay (~150 ms) and the overlay stays up
  for a minimum time (~250 ms), so fast responses never flicker.
- **`loadingInterceptor`** (`shared/interceptors/loading.interceptor.ts`) — feeds
  the service from every **blocking** HTTP request (writes: POST/PUT/PATCH/DELETE);
  safe reads (GET) are left out because they render their own skeletons/spinners.
  `finalize` decrements on success, error and cancellation. Registered first in
  `app.config.ts` so the spinner stays up across a token-refresh retry.
- **`SKIP_LOADING`** (`HttpContextToken`) — marks a write to stay out of the
  overlay. Used by the silent `POST /auth/refresh`; use it for any background call.
- **`LoadingOverlayComponent`** (`shared/components/loading-overlay`) — full-screen
  blocking overlay with an accessible `role="status"` / `aria-live` region, mounted
  once in `AppComponent` so it covers auth, marketing and dashboard. Its z-index
  (1000) stays below SweetAlert2 (1060) so result dialogs remain on top.
- **`SpinnerComponent`** (`shared/components/spinner`) — reusable inline spinner
  (`size` `sm`/`md`/`lg`), inherits `currentColor`, decorative unless a `label` is
  given. Used inside buttons and inline loading states.

## Per-surface feedback

- **Reads** (`httpResource.isLoading()`) — skeleton placeholders that mirror the
  final layout: home (motorcycle grid), statistics (KPI cards), profile, the
  maintenance catalog and the motorcycle summary.
- **Writes and blocking actions** — a small spinner inside the action button plus a
  disabled state while the request is in flight (login, register, password
  recovery/confirm, add/edit motorcycle, add/edit maintenance, register record,
  delete card/maintenance, follow, push, exports, onboarding, landing demo).

## Adding a new request

1. Reads: expose an `isLoading` from the `httpResource` and render a skeleton.
2. Writes: add an `isSubmitting`/`isDeleting` signal around the `subscribe` and put
   `<app-spinner size="sm">` in the triggering button (reset it on error and, when
   the view stays, on success).
3. Background/silent calls: tag the request with `SKIP_LOADING`.

Code and comments are English; user-facing strings use i18n keys (`common.loading`).
