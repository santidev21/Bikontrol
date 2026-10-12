import { Injectable, signal } from '@angular/core';

/** Delay before showing the overlay, so fast responses never flicker. */
const SHOW_DELAY_MS = 150;

/** Minimum time the overlay stays visible once it has been shown. */
const MIN_VISIBLE_MS = 250;

/**
 * Tracks in-flight HTTP requests (via `loadingInterceptor`) and exposes whether
 * the global blocking overlay should be visible. The show delay plus the
 * minimum visible time keep fast requests from flashing the UI.
 */
@Injectable({ providedIn: 'root' })
export class LoadingService {
  private readonly pending = signal(0);
  private readonly visible = signal(false);

  private showTimer?: ReturnType<typeof setTimeout>;
  private hideTimer?: ReturnType<typeof setTimeout>;
  private shownAt = 0;

  /** Raw number of in-flight tracked requests. */
  readonly pendingCount = this.pending.asReadonly();

  /** Whether the blocking overlay is currently visible. */
  readonly isLoading = this.visible.asReadonly();

  /** Registers the start of a tracked request. */
  begin(): void {
    this.pending.update((count) => count + 1);
    // A new request while the overlay is shown must keep it open.
    this.clearHideTimer();

    if (!this.visible() && !this.showTimer) {
      this.showTimer = setTimeout(() => {
        this.showTimer = undefined;
        this.shownAt = Date.now();
        this.visible.set(true);
      }, SHOW_DELAY_MS);
    }
  }

  /** Registers the end of a tracked request (success, error or cancel). */
  end(): void {
    this.pending.update((count) => Math.max(0, count - 1));

    if (this.pending() > 0) return;

    this.clearShowTimer();
    if (!this.visible()) return;

    const elapsed = Date.now() - this.shownAt;
    const remaining = Math.max(0, MIN_VISIBLE_MS - elapsed);

    this.clearHideTimer();
    this.hideTimer = setTimeout(() => {
      this.hideTimer = undefined;
      if (this.pending() === 0) {
        this.visible.set(false);
      }
    }, remaining);
  }

  private clearShowTimer(): void {
    if (this.showTimer) {
      clearTimeout(this.showTimer);
      this.showTimer = undefined;
    }
  }

  private clearHideTimer(): void {
    if (this.hideTimer) {
      clearTimeout(this.hideTimer);
      this.hideTimer = undefined;
    }
  }
}
