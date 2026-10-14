import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../../../auth/services/auth.service';
import { HttpErrorService } from '../../../../../shared/services/http-error.service';
import { SwalService } from '../../../../../shared/services/swal.service';
import { FuelUpsService } from '../../../service/fuel-ups.service';
import { StatisticsService } from '../../../service/statistics.service';
import { FuelUp } from '../../../interfaces/fuel-up.interface';
import { FuelPageComponent } from './fuel-page.component';

function fakeResource<T>(initial?: T, initialError?: string) {
  const value = signal<T | undefined>(initial);
  return {
    value,
    error: signal<Error | undefined>(initialError ? new Error(initialError) : undefined),
    hasValue: () => value() !== undefined,
    isLoading: signal(false),
    status: signal('idle'),
    reload: vi.fn(),
  } as any;
}

const fuelUpMock: FuelUp = {
  id: 'f1',
  motorcycleId: 'm1',
  fueledAt: '2026-06-09T00:00:00.000Z',
  odometerKm: 12000,
  litres: 10.5,
  totalCost: 85000,
  pricePerLitre: 8095.24,
  isFullTank: true,
  station: 'Terpel',
  notes: null,
  createdAt: '2026-06-09T10:00:00.000Z',
};

const consumptionMock: any = {
  motorcycleId: 'm1',
  averageKmPerLitre: 42.5,
  fuelCostPerKm: 0.19,
  costPerKm: 0.25,
  totalFuelCost: 850000,
  totalMaintenanceCost: 200000,
  totalCostOfOwnership: 1050000,
  totalLitres: 120,
  totalDistanceKm: 5100,
  latestKmPerLitre: 30,
  baselineKmPerLitre: 45,
  isConsumptionDrop: true,
  points: [
    {
      fueledAt: '2026-05-01T00:00:00.000Z',
      odometerKm: 7000,
      distanceKm: 500,
      litres: 10,
      cost: 80000,
      kmPerLitre: 50,
    },
    {
      fueledAt: '2026-06-09T00:00:00.000Z',
      odometerKm: 12000,
      distanceKm: 500,
      litres: 16.6,
      cost: 90000,
      kmPerLitre: 30,
    },
  ],
};

describe('FuelPageComponent', () => {
  let fuelUpsServiceMock: any;
  let statisticsServiceMock: any;
  let swalMock: any;
  let routerMock: any;
  let authServiceMock: any;
  let listRes: any;
  let consumptionRes: any;
  let summaryRes: any;

  beforeEach(() => {
    listRes = fakeResource<FuelUp[]>([]);
    consumptionRes = fakeResource(undefined);
    summaryRes = fakeResource(undefined);

    fuelUpsServiceMock = {
      getByMotorcycleResource: vi.fn(() => listRes),
      getConsumptionResource: vi.fn(() => consumptionRes),
      create: vi.fn(() => of(fuelUpMock)),
      update: vi.fn(() => of(fuelUpMock)),
      delete: vi.fn(() => of(undefined)),
    };
    statisticsServiceMock = { getSummaryResource: vi.fn(() => summaryRes) };
    swalMock = {
      error: vi.fn(),
      warning: vi.fn(),
      success: vi.fn().mockReturnValue(Promise.resolve({})),
      confirm: vi.fn().mockReturnValue(Promise.resolve({ isConfirmed: true })),
    };
    routerMock = { navigate: vi.fn() };
    authServiceMock = { isDemo: vi.fn().mockReturnValue(false) };

    TestBed.configureTestingModule({
      imports: [FuelPageComponent],
      providers: [
        { provide: FuelUpsService, useValue: fuelUpsServiceMock },
        { provide: StatisticsService, useValue: statisticsServiceMock },
        { provide: SwalService, useValue: swalMock },
        {
          provide: HttpErrorService,
          useValue: { message: (err: any, fallback: string) => err?.message ?? fallback },
        },
        { provide: AuthService, useValue: authServiceMock },
        { provide: Router, useValue: routerMock },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => 'm1' } } },
        },
      ],
    });
  });

  function create() {
    const fixture = TestBed.createComponent(FuelPageComponent);
    fixture.detectChanges();
    return fixture.componentInstance;
  }

  it('loads the motorcycle id from the route and wires the resources', () => {
    const component = create();

    expect(component.motorcycleId()).toBe('m1');
    expect(fuelUpsServiceMock.getByMotorcycleResource).toHaveBeenCalled();
    expect(fuelUpsServiceMock.getConsumptionResource).toHaveBeenCalled();
  });

  it('exposes the fuel-ups and consumption from the resources', () => {
    listRes = fakeResource<FuelUp[]>([fuelUpMock]);
    consumptionRes = fakeResource(consumptionMock);

    const component = create();

    expect(component.fuelUps()).toEqual([fuelUpMock]);
    expect(component.consumption()).toEqual(consumptionMock);
    expect(component.points()).toHaveLength(2);
    expect(component.maxKmPerLitre()).toBe(50);
    expect(component.hasTrend()).toBe(true);
    expect(component.hasConsumptionDrop()).toBe(true);
  });

  it('redirects home when there is no motorcycle context', () => {
    TestBed.overrideProvider(ActivatedRoute, {
      useValue: { snapshot: { paramMap: { get: () => null } } },
    });

    const component = create();

    expect(component.motorcycleId()).toBeUndefined();
    expect(swalMock.warning).toHaveBeenCalled();
    expect(routerMock.navigate).toHaveBeenCalledWith(['/dashboard/home']);
  });

  it('defaults the form to today and a full tank', () => {
    const component = create();

    expect(component.form.get('isFullTank')?.value).toBe(true);
    expect(component.form.get('fueledAt')?.value).toBe(new Date().toISOString().split('T')[0]);
    expect(component.isEditing()).toBe(false);
  });

  it('creates a fuel-up and reloads the data', () => {
    const component = create();
    component.form.patchValue({
      fueledAt: '2026-06-09',
      odometerKm: 12000,
      litres: 10.5,
      totalCost: 85000,
      isFullTank: true,
      station: ' Terpel ',
      notes: '',
    });

    component.onSubmit();

    expect(fuelUpsServiceMock.create).toHaveBeenCalledWith({
      motorcycleId: 'm1',
      fueledAt: new Date('2026-06-09T00:00:00').toISOString(),
      odometerKm: 12000,
      litres: 10.5,
      totalCost: 85000,
      isFullTank: true,
      station: 'Terpel',
      notes: null,
    });
    expect(listRes.reload).toHaveBeenCalled();
    expect(consumptionRes.reload).toHaveBeenCalled();
    expect(swalMock.success).toHaveBeenCalled();
    expect(component.isSubmitting()).toBe(false);
  });

  it('does not create a fuel-up when the form is invalid', () => {
    const component = create();
    component.form.patchValue({ fueledAt: '2026-06-09', odometerKm: null, litres: null });

    component.onSubmit();

    expect(fuelUpsServiceMock.create).not.toHaveBeenCalled();
    expect(swalMock.warning).toHaveBeenCalled();
    expect(component.submitted()).toBe(true);
    expect(component.isInvalid('odometerKm')).toBe(true);
  });

  it('patches the form when editing and updates the fuel-up', () => {
    const component = create();

    component.startEdit(fuelUpMock);

    expect(component.isEditing()).toBe(true);
    expect(component.editingId()).toBe('f1');
    expect(component.form.get('fueledAt')?.value).toBe('2026-06-09');
    expect(component.form.get('litres')?.value).toBe(10.5);
    expect(component.form.get('isFullTank')?.value).toBe(true);

    component.onSubmit();

    expect(fuelUpsServiceMock.update).toHaveBeenCalledWith(
      'f1',
      expect.objectContaining({ motorcycleId: 'm1', litres: 10.5, station: 'Terpel' }),
    );
    expect(fuelUpsServiceMock.create).not.toHaveBeenCalled();
    expect(component.isEditing()).toBe(false);
  });

  it('cancels editing and restores the defaults', () => {
    const component = create();
    component.startEdit(fuelUpMock);

    component.cancelEdit();

    expect(component.isEditing()).toBe(false);
    expect(component.form.get('isFullTank')?.value).toBe(true);
    expect(component.form.get('station')?.value).toBe('');
  });

  it('surfaces an error when saving fails', () => {
    fuelUpsServiceMock.create.mockReturnValue(throwError(() => new Error('boom')));
    const component = create();
    component.form.patchValue({
      fueledAt: '2026-06-09',
      odometerKm: 12000,
      litres: 10.5,
      totalCost: 85000,
    });

    component.onSubmit();

    expect(swalMock.error).toHaveBeenCalledWith('Error', 'boom');
    expect(component.isSubmitting()).toBe(false);
  });

  it('deletes a fuel-up after confirming', async () => {
    const component = create();

    component.confirmDelete(fuelUpMock);
    await Promise.resolve();

    expect(fuelUpsServiceMock.delete).toHaveBeenCalledWith('f1');
    expect(listRes.reload).toHaveBeenCalled();
    expect(swalMock.success).toHaveBeenCalled();
  });

  it('does not delete when the confirmation is dismissed', async () => {
    swalMock.confirm.mockReturnValue(Promise.resolve({ isConfirmed: false }));
    const component = create();

    component.confirmDelete(fuelUpMock);
    await Promise.resolve();

    expect(fuelUpsServiceMock.delete).not.toHaveBeenCalled();
  });

  it('surfaces an error when the deletion fails', () => {
    fuelUpsServiceMock.delete.mockReturnValue(throwError(() => new Error('nope')));
    const component = create();

    component.deleteFuelUp('f1');

    expect(swalMock.error).toHaveBeenCalledWith('Error', 'nope');
    expect(component.deletingId()).toBeNull();
  });

  it('blocks all writes for demo users', () => {
    authServiceMock.isDemo.mockReturnValue(true);
    const component = create();

    component.onSubmit();
    component.startEdit(fuelUpMock);
    component.confirmDelete(fuelUpMock);

    expect(component.isDemo()).toBe(true);
    expect(fuelUpsServiceMock.create).not.toHaveBeenCalled();
    expect(fuelUpsServiceMock.update).not.toHaveBeenCalled();
    expect(fuelUpsServiceMock.delete).not.toHaveBeenCalled();
    expect(component.isEditing()).toBe(false);
  });

  it('shows errors when the resources fail to load', () => {
    listRes = fakeResource<FuelUp[]>(undefined, 'list boom');
    consumptionRes = fakeResource(undefined, 'consumption boom');

    create();

    expect(swalMock.error).toHaveBeenCalledWith('Error', 'list boom');
    expect(swalMock.error).toHaveBeenCalledWith('Error', 'consumption boom');
  });

  it('flags an overspend banner from the statistics budget', () => {
    summaryRes = fakeResource({
      budget: {
        monthlyBudget: 100000,
        annualBudget: 1000000,
        monthlySpend: 150000,
        annualSpend: 150000,
        isMonthlyOverBudget: true,
        isAnnualOverBudget: false,
      },
    });

    const component = create();

    expect(component.budgetOver()).toBe(true);
    expect(component.budgetOverMessage()).toContain('mes');
  });

  it('reports the annual overspend period', () => {
    summaryRes = fakeResource({
      budget: {
        monthlyBudget: null,
        annualBudget: 1000000,
        monthlySpend: 0,
        annualSpend: 1500000,
        isMonthlyOverBudget: false,
        isAnnualOverBudget: true,
      },
    });

    const component = create();

    expect(component.budgetOver()).toBe(true);
    expect(component.budgetOverMessage()).toContain('año');
  });

  it('does not flag an overspend banner without a budget', () => {
    const component = create();

    expect(component.budgetOver()).toBe(false);
  });

  it('computes bar widths relative to the best segment', () => {
    const component = create();

    expect(component.barWidth(50, 50)).toBe(100);
    expect(component.barWidth(25, 50)).toBe(50);
    expect(component.barWidth(null, 50)).toBe(0);
    expect(component.barWidth(10, 0)).toBe(0);
  });

  it('navigates back to the motorcycle summary', () => {
    const component = create();

    component.goToSummary();

    expect(routerMock.navigate).toHaveBeenCalledWith(['/dashboard/motorcycles/summary'], {
      queryParams: { motorcycleId: 'm1' },
    });
  });

  it('exposes inline validation helpers', () => {
    const component = create();
    component.form.get('litres')?.markAsTouched();

    expect(component.hasError('litres', 'required')).toBe(true);
    expect(component.hasError('station', 'required')).toBe(false);
    expect(component.isInvalid('station')).toBe(false);
  });
});
