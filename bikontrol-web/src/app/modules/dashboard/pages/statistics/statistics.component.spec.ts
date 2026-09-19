import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import type { Mock } from 'vitest';
import { AuthService } from '../../../auth/services/auth.service';
import { HttpErrorService } from '../../../../shared/services/http-error.service';
import { SwalService } from '../../../../shared/services/swal.service';
import { StatisticsComponent } from './statistics.component';

const summaryMock: any = {
  totalMotorcycles: 2,
  totalKm: 20000,
  totalMaintenanceRecords: 4,
  overdueCount: 1,
  dueSoonCount: 1,
  lastActivityAt: '2026-09-10T00:00:00Z',
  health: [
    { bucket: 'Vencido', count: 1 },
    { bucket: 'Crítico', count: 1 },
    { bucket: 'Próximo', count: 1 },
    { bucket: 'OK', count: 1 }
  ],
  kmByMotorcycle: [
    { motorcycleId: 'm1', name: 'Negra', km: 8000 },
    { motorcycleId: 'm2', name: 'Roja', km: 12000 }
  ],
  recordsByType: [{ name: 'Aceite', count: 3 }],
  last6Months: [{ yearMonth: '2026-09', count: 2 }]
};

describe('StatisticsComponent', () => {
  let httpMock: HttpTestingController;
  let swalMock: { error: Mock; success: Mock };

  beforeEach(() => {
    swalMock = { error: vi.fn(), success: vi.fn() };

    TestBed.configureTestingModule({
      imports: [StatisticsComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        HttpErrorService,
        { provide: SwalService, useValue: swalMock },
        { provide: AuthService, useValue: { isDemo: () => false } }
      ]
    });

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  function create() {
    return TestBed.createComponent(StatisticsComponent);
  }

  it('loads the summary into the resource', async () => {
    const fixture = create();
    fixture.detectChanges();

    httpMock.expectOne(r => r.url.endsWith('/statistics/summary')).flush(summaryMock);
    await fixture.whenStable();

    expect(fixture.componentInstance.summary.value()).toEqual(summaryMock);
  });

  it('resolves max values from the summary', async () => {
    const fixture = create();
    fixture.detectChanges();

    httpMock.expectOne(r => r.url.endsWith('/statistics/summary')).flush(summaryMock);
    await fixture.whenStable();

    const component = fixture.componentInstance;
    expect(component.maxKm()).toBe(12000);
    expect(component.maxTypeCount()).toBe(3);
    expect(component.maxMonthCount()).toBe(2);
    expect(component.maxHealthCount()).toBe(1);
  });

  it('shows an error when the summary fails to load', async () => {
    const fixture = create();
    fixture.detectChanges();

    httpMock.expectOne(r => r.url.endsWith('/statistics/summary'))
      .flush('boom', { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.summary.hasValue()).toBe(false);
    expect(fixture.componentInstance.summaryData()).toBeUndefined();
    expect(swalMock.error).toHaveBeenCalled();
  });

  it('computes bar widths relative to the max', () => {
    const component = create().componentInstance;

    expect(component.barWidth(50, 100)).toBe(50);
    expect(component.barWidth(0, 100)).toBe(0);
    expect(component.barWidth(10, 0)).toBe(0);
  });

  it('maps health buckets to colors', () => {
    const component = create().componentInstance;

    expect(component.healthColor('Vencido')).toBe('#ef4444');
    expect(component.healthColor('OK')).toBe('#10b981');
    expect(component.healthColor('Desconocido')).toBe('#6b7280');
  });

  it('formats month labels in spanish', () => {
    const component = create().componentInstance;

    expect(component.monthLabel('2026-09')).toBe('sep');
    expect(component.monthLabel('2026-01')).toBe('ene');
    expect(component.monthLabel('invalid')).toBe('invalid');
  });
});
