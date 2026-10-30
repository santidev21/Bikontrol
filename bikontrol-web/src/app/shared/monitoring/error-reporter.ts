import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, InjectionToken, inject } from '@angular/core';
import { environment } from '@env/environment';

/** The few Sentry SDK members used here (keeps the SDK out of the main bundle and easy to fake in tests). */
export interface SentrySdk {
  init(options: Record<string, unknown>): void;
  captureException(error: unknown): void;
}

export interface ErrorReportingConfig {
  dsn: string;
  environment: string;
  release: string;
}

/** Defaults to the build-time values written by `scripts/set-env.js`. */
export const ERROR_REPORTING_CONFIG = new InjectionToken<ErrorReportingConfig>(
  'ERROR_REPORTING_CONFIG',
  {
    providedIn: 'root',
    factory: () => ({
      dsn: environment.sentryDsn,
      environment: environment.production ? 'production' : 'development',
      release: environment.appVersion,
    }),
  },
);

/** Loads the SDK lazily: a build without a DSN never downloads it. */
export const SENTRY_SDK_LOADER = new InjectionToken<() => Promise<SentrySdk>>('SENTRY_SDK_LOADER', {
  providedIn: 'root',
  factory: () => () => import('@sentry/browser') as Promise<SentrySdk>,
});

/**
 * Drops the query string and fragment. The app puts one-time secrets in URLs
 * (`/reset-password?token=…`, `/confirm-email?token=…`, `/unsubscribe?token=…`) and
 * signed media URLs carry a signature; none of that may reach an error tracker.
 */
export function scrubUrl(url: string): string {
  return url.split(/[?#]/, 1)[0];
}

const STALE_DEPLOY =
  /Failed to fetch dynamically imported module|error loading dynamically imported module|Loading chunk [\w-]+ failed|Importing a module script failed/i;

/**
 * Whether an error is worth a report. HTTP failures are expected control flow here (the
 * backend has its own Sentry), and a missing lazy chunk just means the user has a tab
 * open from before a deploy.
 */
export function shouldReport(error: unknown): boolean {
  if (error instanceof HttpErrorResponse) return false;
  const message = error instanceof Error ? error.message : String(error ?? '');
  return !STALE_DEPLOY.test(message);
}

interface SentryEvent {
  request?: { url?: string; headers?: Record<string, string> };
  breadcrumbs?: SentryBreadcrumb[];
}

interface SentryBreadcrumb {
  data?: Record<string, unknown>;
}

const URL_KEYS = ['url', 'from', 'to'];

export function scrubBreadcrumb<T extends SentryBreadcrumb>(breadcrumb: T): T {
  if (!breadcrumb.data) return breadcrumb;
  const data = { ...breadcrumb.data };
  for (const key of URL_KEYS) {
    if (typeof data[key] === 'string') data[key] = scrubUrl(data[key] as string);
  }
  return { ...breadcrumb, data };
}

export function scrubEvent<T extends SentryEvent>(event: T): T {
  if (event.request?.url) event.request = { ...event.request, url: scrubUrl(event.request.url) };
  if (event.request?.headers) {
    const { Referer, referer, ...headers } = event.request.headers;
    void Referer;
    void referer;
    event.request = { ...event.request, headers };
  }
  if (event.breadcrumbs) event.breadcrumbs = event.breadcrumbs.map(scrubBreadcrumb);
  return event;
}

/** Errors raised before the SDK finished loading are kept (bounded) and flushed after init. */
const MAX_PENDING = 20;

@Injectable({ providedIn: 'root' })
export class ErrorReporter {
  private readonly loadSdk = inject(SENTRY_SDK_LOADER);
  private readonly config = inject(ERROR_REPORTING_CONFIG);

  private sdk: SentrySdk | null = null;
  private pending: unknown[] = [];
  private started = false;

  /** True when this build has a DSN. Without one every call is a no-op. */
  get enabled(): boolean {
    return !!this.config.dsn;
  }

  async init(): Promise<void> {
    if (!this.enabled || this.started) return;
    this.started = true;

    try {
      const sdk = await this.loadSdk();
      sdk.init({
        dsn: this.config.dsn,
        environment: this.config.environment,
        release: this.config.release,
        // Errors only: no performance traces, no session replay, no user data.
        tracesSampleRate: 0,
        sendDefaultPii: false,
        ignoreErrors: [
          'ResizeObserver loop limit exceeded',
          'ResizeObserver loop completed with undelivered notifications',
        ],
        denyUrls: [
          /extensions\//i,
          /^chrome-extension:\/\//i,
          /^moz-extension:\/\//i,
          /^safari-extension:\/\//i,
        ],
        beforeSend: (event: SentryEvent) => scrubEvent(event),
        beforeBreadcrumb: (breadcrumb: SentryBreadcrumb) => scrubBreadcrumb(breadcrumb),
      });
      this.sdk = sdk;
      this.pending.splice(0).forEach((error) => sdk.captureException(error));
    } catch {
      // The tracker must never break the app (ad blockers block the chunk's host).
      this.pending = [];
    }
  }

  report(error: unknown): void {
    if (!this.enabled || !shouldReport(error)) return;

    if (this.sdk) {
      this.sdk.captureException(error);
    } else if (this.pending.length < MAX_PENDING) {
      this.pending.push(error);
    }
  }
}
