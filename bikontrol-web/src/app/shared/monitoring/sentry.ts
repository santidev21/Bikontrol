import { EnvironmentProviders, ErrorHandler, Provider } from '@angular/core';
import * as Sentry from '@sentry/angular';
import { environment } from '../../../environments/environment';

/**
 * Starts browser error tracking when a DSN was baked into the build. Without
 * one nothing is initialised and no data leaves the browser. Tracing and session
 * replay are intentionally off: only unhandled errors are reported.
 */
export function initSentry(dsn: string = environment.sentryDsn): boolean {
  if (!dsn) return false;
  Sentry.init({
    dsn,
    environment: environment.production ? 'production' : 'development',
    release: `bikontrol-web@${environment.appVersion}`,
    tracesSampleRate: 0,
  });
  return true;
}

/** Routes Angular's unhandled errors to Sentry; the default handler stays when it is off. */
export function provideErrorTracking(
  dsn: string = environment.sentryDsn,
): (Provider | EnvironmentProviders)[] {
  return dsn
    ? [{ provide: ErrorHandler, useValue: Sentry.createErrorHandler({ showDialog: false }) }]
    : [];
}
