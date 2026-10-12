import { TestBed } from '@angular/core/testing';
import { LoadingService } from './loading.service';

describe('LoadingService', () => {
  let service: LoadingService;

  beforeEach(() => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({});
    service = TestBed.inject(LoadingService);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('stays hidden until the show delay elapses', () => {
    service.begin();
    expect(service.isLoading()).toBe(false);

    vi.advanceTimersByTime(149);
    expect(service.isLoading()).toBe(false);

    vi.advanceTimersByTime(1);
    expect(service.isLoading()).toBe(true);
  });

  it('never shows for a request that finishes before the delay', () => {
    service.begin();
    service.end();

    vi.advanceTimersByTime(1000);

    expect(service.isLoading()).toBe(false);
  });

  it('keeps the overlay visible until the last request settles', () => {
    service.begin();
    service.begin();

    vi.advanceTimersByTime(150);
    expect(service.isLoading()).toBe(true);

    service.end();
    vi.advanceTimersByTime(1000);
    expect(service.isLoading()).toBe(true);

    service.end();
    vi.advanceTimersByTime(250);
    expect(service.isLoading()).toBe(false);
  });

  it('holds the overlay for the minimum visible time', () => {
    service.begin();
    vi.advanceTimersByTime(150);
    expect(service.isLoading()).toBe(true);

    service.end();
    vi.advanceTimersByTime(249);
    expect(service.isLoading()).toBe(true);

    vi.advanceTimersByTime(1);
    expect(service.isLoading()).toBe(false);
  });

  it('cancels a pending hide when a new request starts', () => {
    service.begin();
    vi.advanceTimersByTime(150);
    service.end();

    // New request before the hide timer fires.
    service.begin();
    vi.advanceTimersByTime(1000);
    expect(service.isLoading()).toBe(true);

    service.end();
    vi.advanceTimersByTime(250);
    expect(service.isLoading()).toBe(false);
  });

  it('never counts below zero', () => {
    service.end();
    expect(service.pendingCount()).toBe(0);
  });
});
