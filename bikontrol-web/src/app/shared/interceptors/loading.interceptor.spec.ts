import {
  HttpClient,
  HttpContext,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { LoadingService } from '../services/loading.service';
import { loadingInterceptor, SKIP_LOADING } from './loading.interceptor';

describe('loadingInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let loading: LoadingService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([loadingInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    loading = TestBed.inject(LoadingService);
  });

  afterEach(() => httpMock.verify());

  it('tracks a write while it is in flight and releases it on success', () => {
    http.post('/api/thing', {}).subscribe();

    expect(loading.pendingCount()).toBe(1);

    httpMock.expectOne('/api/thing').flush({});

    expect(loading.pendingCount()).toBe(0);
  });

  it('releases the request on error', () => {
    http.delete('/api/thing').subscribe({ error: () => undefined });

    expect(loading.pendingCount()).toBe(1);

    httpMock.expectOne('/api/thing').flush('boom', { status: 500, statusText: 'Server Error' });

    expect(loading.pendingCount()).toBe(0);
  });

  it('leaves safe reads (GET) to their own skeletons', () => {
    http.get('/api/thing').subscribe();

    expect(loading.pendingCount()).toBe(0);

    httpMock.expectOne('/api/thing').flush({});

    expect(loading.pendingCount()).toBe(0);
  });

  it('ignores writes flagged with SKIP_LOADING', () => {
    http
      .post('/api/thing', {}, { context: new HttpContext().set(SKIP_LOADING, true) })
      .subscribe();

    expect(loading.pendingCount()).toBe(0);

    httpMock.expectOne('/api/thing').flush({});

    expect(loading.pendingCount()).toBe(0);
  });
});
