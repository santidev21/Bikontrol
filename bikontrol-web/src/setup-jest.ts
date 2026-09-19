// Jest setup for Angular tests (jest-preset-angular + zoneless TestBed).
import { setupZonelessTestEnv } from 'jest-preset-angular/setup-env/zoneless';

setupZonelessTestEnv();

// Provide basic globals if needed by some libraries.
declare const global: any;
if (typeof global.TextEncoder === 'undefined') {
  global.TextEncoder = require('util').TextEncoder;
  global.TextDecoder = require('util').TextDecoder;
}

// jsdom does not implement IntersectionObserver, required by @defer (on viewport).
if (typeof global.IntersectionObserver === 'undefined') {
  class MockIntersectionObserver {
    readonly root = null;
    readonly rootMargin = '';
    readonly thresholds: ReadonlyArray<number> = [];
    observe(): void {}
    unobserve(): void {}
    disconnect(): void {}
    takeRecords(): any[] {
      return [];
    }
  }
  global.IntersectionObserver = MockIntersectionObserver;
}
