import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import {
  ERROR_REPORTING_CONFIG,
  ErrorReporter,
  SENTRY_SDK_LOADER,
  SentrySdk,
  scrubBreadcrumb,
  scrubEvent,
  scrubUrl,
  shouldReport,
} from './error-reporter';
import { AppErrorHandler } from './app-error-handler';

describe('scrubUrl', () => {
  it('drops the query string and the fragment', () => {
    expect(scrubUrl('https://app.test/reset-password?token=SECRET&email=a@b.c')).toBe(
      'https://app.test/reset-password',
    );
    expect(scrubUrl('/unsubscribe?token=abc#frag')).toBe('/unsubscribe');
    expect(scrubUrl('/api/media/attachments/1?exp=1&sig=s')).toBe('/api/media/attachments/1');
    expect(scrubUrl('/dashboard')).toBe('/dashboard');
  });
});

describe('shouldReport', () => {
  it('ignores HTTP failures (the backend reports its own) and stale lazy chunks', () => {
    expect(shouldReport(new HttpErrorResponse({ status: 500 }))).toBe(false);
    expect(shouldReport(new HttpErrorResponse({ status: 0 }))).toBe(false);
    expect(
      shouldReport(
        new TypeError('Failed to fetch dynamically imported module: https://x/chunk.js'),
      ),
    ).toBe(false);
    expect(shouldReport(new Error('Loading chunk 42 failed.'))).toBe(false);
  });

  it('reports real bugs and non-Error rejections', () => {
    expect(shouldReport(new TypeError("Cannot read properties of undefined (reading 'x')"))).toBe(
      true,
    );
    expect(shouldReport('boom')).toBe(true);
    expect(shouldReport(undefined)).toBe(true);
  });
});

describe('scrubEvent / scrubBreadcrumb', () => {
  it('cleans the request url, the referer and every url in the breadcrumbs', () => {
    const event = scrubEvent({
      request: {
        url: 'https://app.test/confirm-email?token=SECRET&email=a@b.c',
        headers: { Referer: 'https://app.test/reset-password?token=SECRET', 'User-Agent': 'UA' },
      },
      breadcrumbs: [
        { data: { from: '/login?x=1', to: '/reset-password?token=SECRET' } },
        { data: { url: '/api/media/motorcycles/1?v=a&exp=1&sig=s', method: 'GET' } },
        {},
      ],
    });

    expect(event.request?.url).toBe('https://app.test/confirm-email');
    expect(event.request?.headers).toEqual({ 'User-Agent': 'UA' });
    expect(event.breadcrumbs?.[0].data).toEqual({ from: '/login', to: '/reset-password' });
    expect(event.breadcrumbs?.[1].data).toEqual({ url: '/api/media/motorcycles/1', method: 'GET' });
    expect(event.breadcrumbs?.[2]).toEqual({});
    expect(JSON.stringify(event)).not.toContain('SECRET');
  });

  it('does not mutate the original breadcrumb data', () => {
    const original = { data: { url: '/a?token=1' } };
    scrubBreadcrumb(original);
    expect(original.data.url).toBe('/a?token=1');
  });
});

describe('ErrorReporter', () => {
  let sdk: { init: ReturnType<typeof vi.fn>; captureException: ReturnType<typeof vi.fn> };
  let loader: ReturnType<typeof vi.fn>;

  function create(dsn: string): ErrorReporter {
    sdk = { init: vi.fn(), captureException: vi.fn() };
    loader = vi.fn(() => Promise.resolve(sdk as SentrySdk));
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        { provide: SENTRY_SDK_LOADER, useValue: loader },
        {
          provide: ERROR_REPORTING_CONFIG,
          useValue: { dsn, environment: 'production', release: '1.2.3' },
        },
      ],
    });
    return TestBed.inject(ErrorReporter);
  }

  it('is a complete no-op without a DSN: the SDK is never even loaded', async () => {
    const reporter = create('');

    await reporter.init();
    reporter.report(new Error('boom'));

    expect(reporter.enabled).toBe(false);
    expect(loader).not.toHaveBeenCalled();
    expect(sdk.captureException).not.toHaveBeenCalled();
  });

  it('initialises the SDK once, without PII, performance traces or replay', async () => {
    const reporter = create('https://key@o1.ingest.sentry.io/1');

    await reporter.init();
    await reporter.init();

    expect(loader).toHaveBeenCalledTimes(1);
    expect(sdk.init).toHaveBeenCalledTimes(1);
    const options = sdk.init.mock.calls[0][0];
    expect(options).toMatchObject({
      dsn: 'https://key@o1.ingest.sentry.io/1',
      environment: 'production',
      release: '1.2.3',
      tracesSampleRate: 0,
      sendDefaultPii: false,
    });
    expect(options.beforeSend({ request: { url: '/x?token=S' } }).request.url).toBe('/x');
  });

  it('captures reportable errors and skips filtered ones', async () => {
    const reporter = create('https://key@o1.ingest.sentry.io/1');
    await reporter.init();

    const bug = new TypeError('real bug');
    reporter.report(bug);
    reporter.report(new HttpErrorResponse({ status: 500 }));

    expect(sdk.captureException).toHaveBeenCalledTimes(1);
    expect(sdk.captureException).toHaveBeenCalledWith(bug);
  });

  it('keeps errors raised before the SDK loaded and flushes them after init', async () => {
    const reporter = create('https://key@o1.ingest.sentry.io/1');
    const early = new Error('during startup');

    reporter.report(early);
    expect(sdk.captureException).not.toHaveBeenCalled();
    await reporter.init();

    expect(sdk.captureException).toHaveBeenCalledWith(early);
  });

  it('bounds the early buffer', async () => {
    const reporter = create('https://key@o1.ingest.sentry.io/1');

    for (let i = 0; i < 50; i++) reporter.report(new Error(`e${i}`));
    await reporter.init();

    expect(sdk.captureException).toHaveBeenCalledTimes(20);
  });

  it('never breaks the app when the SDK cannot be loaded (e.g. blocked by an ad blocker)', async () => {
    const reporter = create('https://key@o1.ingest.sentry.io/1');
    loader.mockRejectedValue(new Error('blocked'));

    await expect(reporter.init()).resolves.toBeUndefined();
    expect(() => reporter.report(new Error('x'))).not.toThrow();
  });
});

describe('AppErrorHandler', () => {
  it('logs to the console and forwards the error to the reporter', () => {
    const report = vi.fn();
    const logged = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [{ provide: ErrorReporter, useValue: { report } }],
    });
    const handler = TestBed.runInInjectionContext(() => new AppErrorHandler());
    const error = new Error('kaboom');

    handler.handleError(error);

    expect(logged).toHaveBeenCalledWith(error);
    expect(report).toHaveBeenCalledWith(error);
    logged.mockRestore();
  });
});
