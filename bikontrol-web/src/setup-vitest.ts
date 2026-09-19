// Global setup for Vitest. The Angular TestBed is initialized by the
// @angular/build:unit-test runner (zoneless), so nothing to do here except
// polyfills that jsdom does not provide.

// jsdom does not implement IntersectionObserver, required by @defer (on viewport).
if (typeof (globalThis as any).IntersectionObserver === 'undefined') {
  class MockIntersectionObserver {
    readonly root = null;
    readonly rootMargin = '';
    readonly thresholds: ReadonlyArray<number> = [];
    observe(): void {}
    unobserve(): void {}
    disconnect(): void {}
    takeRecords(): unknown[] {
      return [];
    }
  }
  (globalThis as any).IntersectionObserver = MockIntersectionObserver;
}

// jsdom does not implement matchMedia; SweetAlert2 reads it when rendering icons.
if (typeof (globalThis as any).matchMedia === 'undefined') {
  (globalThis as any).matchMedia = (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false
  });
}
