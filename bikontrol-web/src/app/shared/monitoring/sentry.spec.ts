import { ErrorHandler } from '@angular/core';
import { describe, expect, it } from 'vitest';
import { initSentry, provideErrorTracking } from './sentry';

describe('error tracking', () => {
  it('stays off without a DSN', () => {
    expect(initSentry('')).toBe(false);
    expect(provideErrorTracking('')).toEqual([]);
  });

  it('replaces the ErrorHandler when a DSN is set', () => {
    const providers = provideErrorTracking('https://key@o0.ingest.sentry.io/1') as {
      provide: unknown;
    }[];
    expect(providers).toHaveLength(1);
    expect(providers[0].provide).toBe(ErrorHandler);
  });
});
