import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../../../../auth/services/auth.service';
import { Motorcycle } from '../../../interfaces/motorcycle.interface';
import { HttpErrorService } from '../../../../../shared/services/http-error.service';
import { SwalService } from '../../../../../shared/services/swal.service';
import { MaintenanceService } from '../../../service/maintenance.service';
import { MotorcyclesService } from '../../../service/motorcycles.service';
import { MotorcycleSummaryComponent } from './motorcycle-summary.component';

function fakeResource<T>(initial?: T) {
  const value = signal<T | undefined>(initial);
  return {
    value,
    hasValue: () => value() !== undefined,
    error: signal<Error | undefined>(undefined),
    isLoading: signal(false),
    status: signal('idle'),
    reload: jest.fn()
  } as any;
}

describe('MotorcycleSummaryComponent', () => {
  let motorcyclesServiceMock: any;
  let maintenanceServiceMock: any;
  let swalMock: any;

  beforeEach(() => {
    motorcyclesServiceMock = {
      getCurrentKmResource: jest.fn(() => fakeResource()),
      getById: jest.fn(),
      addKmHistory: jest.fn(() => of(undefined)),
      rollbackLastKm: jest.fn(() => of(undefined))
    };
    maintenanceServiceMock = {
      getUpcomingResource: jest.fn(() => fakeResource()),
      getRecordsResource: jest.fn(() => fakeResource())
    };
    swalMock = {
      error: jest.fn(),
      success: jest.fn().mockReturnValue(Promise.resolve({})),
      confirm: jest.fn().mockReturnValue(Promise.resolve({ isConfirmed: true }))
    };

    TestBed.configureTestingModule({
      imports: [MotorcycleSummaryComponent],
      providers: [
        { provide: MotorcyclesService, useValue: motorcyclesServiceMock },
        { provide: MaintenanceService, useValue: maintenanceServiceMock },
        { provide: SwalService, useValue: swalMock },
        {
          provide: HttpErrorService,
          useValue: { message: (err: any, fallback: string) => err?.message ?? fallback }
        },
        { provide: AuthService, useValue: { isDemo: () => false } },
        { provide: Router, useValue: { getCurrentNavigation: () => null, navigate: jest.fn() } },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: { get: () => null } } } }
      ]
    });
  });

  function create() {
    return TestBed.createComponent(MotorcycleSummaryComponent).componentInstance;
  }

  it('reports whether a maintenance can be registered', () => {
    const component = create();

    component.upcomingMaintenances.set([]);
    expect(component.canRegisterMaintenance()).toBe(false);

    component.upcomingMaintenances.set([{ name: 'Aceite' } as any]);
    expect(component.canRegisterMaintenance()).toBe(true);
  });

  it('opens and closes the edit km modal around the current km', () => {
    const component = create();
    component.currentKm.set(1000);

    component.openEditKmModal();
    expect(component.isEditKmModalOpen()).toBe(true);
    expect(component.editableKm()).toBe(1000);

    component.closeEditKmModal();
    expect(component.isEditKmModalOpen()).toBe(false);
  });

  it('does not call addKmHistory when the new km is lower than the current one', () => {
    const component = create();
    component.motorcycle.set({ id: 'm1' } as Motorcycle);
    component.currentKm.set(1000);
    component.editableKm.set(900);

    component.saveKm();

    expect(motorcyclesServiceMock.addKmHistory).not.toHaveBeenCalled();
    expect(swalMock.error).toHaveBeenCalled();
  });

  it('calls addKmHistory when the new km is valid', () => {
    const component = create();
    component.motorcycle.set({ id: 'm1' } as Motorcycle);
    component.currentKm.set(1000);
    component.editableKm.set(1400);

    component.saveKm();

    expect(motorcyclesServiceMock.addKmHistory).toHaveBeenCalledWith('m1', 1400);
  });

  it('calls rollbackLastKm when the user confirms', async () => {
    const component = create();
    component.motorcycle.set({ id: 'm1' } as Motorcycle);
    component.currentKm.set(1300);

    component.rollbackLastKm();
    await Promise.resolve();

    expect(motorcyclesServiceMock.rollbackLastKm).toHaveBeenCalledWith('m1', 1300);
  });
});
