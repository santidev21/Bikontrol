import { TestBed } from '@angular/core/testing';
import { FormBuilder } from '@angular/forms';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { OnboardingComponent } from './onboarding.component';
import { Maintenance } from '../../interfaces/maintenance.interface';
import { MaintenanceService } from '../../service/maintenance.service';
import { MotorcyclesService } from '../../service/motorcycles.service';
import { AuthService } from '../../../auth/services/auth.service';
import { SwalService } from '../../../../shared/services/swal.service';
import { HttpErrorService } from '../../../../shared/services/http-error.service';

function maintenance(overrides: Partial<Maintenance>): Maintenance {
  return {
    id: 'm-1',
    motorcycleId: '',
    name: 'Cambio de Aceite',
    trackingType: 'Km',
    kmInterval: 1500,
    timeIntervalWeeks: 6,
    isEnabled: true,
    isSystem: true,
    ...overrides,
  };
}

describe('OnboardingComponent', () => {
  let component: OnboardingComponent;
  let motorcyclesServiceMock: any;
  let maintenanceServiceMock: any;
  let authServiceMock: any;
  let routerMock: any;
  let swalMock: any;

  const defaults: Maintenance[] = [
    maintenance({ id: 'd-aceite', name: 'Cambio de Aceite', trackingType: 'Km', kmInterval: 1500 }),
    maintenance({
      id: 'd-cadena',
      name: 'Lubricación y Limpieza de Cadena',
      trackingType: 'Time',
      kmInterval: null,
      timeIntervalWeeks: 2,
    }),
    maintenance({ id: 'd-bujia', name: 'Bujía', trackingType: 'Km', kmInterval: 4000 }),
  ];

  function setup(isDemo = false) {
    motorcyclesServiceMock = { addMotorcycle: vi.fn() };
    maintenanceServiceMock = {
      getDefaultMaintenance: vi.fn().mockReturnValue(of(defaults)),
      followDefaultMaintenance: vi.fn(),
    };
    authServiceMock = { isDemo: () => isDemo };
    routerMock = { navigate: vi.fn() };
    swalMock = {
      error: vi.fn(),
      warning: vi.fn().mockResolvedValue(true),
      success: vi.fn().mockResolvedValue(true),
    };

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        { provide: FormBuilder, useValue: new FormBuilder() },
        { provide: MotorcyclesService, useValue: motorcyclesServiceMock },
        { provide: MaintenanceService, useValue: maintenanceServiceMock },
        { provide: AuthService, useValue: authServiceMock },
        { provide: Router, useValue: routerMock },
        { provide: SwalService, useValue: swalMock },
        {
          provide: HttpErrorService,
          useValue: { message: (e: any, f: string) => e?.message ?? f },
        },
      ],
    });
    component = TestBed.runInInjectionContext(() => new OnboardingComponent());
  }

  function fillValidForm() {
    component.motorcycleForm.setValue({
      name: 'XTZ 150',
      brand: 'Yamaha',
      year: 2024,
      nickname: 'La azul',
      km: 1000,
      displacement: 150,
      plate: 'ABC123',
    });
  }

  const flush = async () => {
    await Promise.resolve();
    await Promise.resolve();
  };

  beforeEach(() => setup());

  it('creates the expected motorcycle form controls', () => {
    for (const field of ['name', 'brand', 'year', 'nickname', 'km', 'displacement', 'plate']) {
      expect(component.motorcycleForm.contains(field)).toBe(true);
    }
  });

  it('loads the plan and pre-selects the recommended defaults', () => {
    component.ngOnInit();

    expect(maintenanceServiceMock.getDefaultMaintenance).toHaveBeenCalled();
    expect(component.loadingPlan()).toBe(false);
    expect(component.selectedCount).toBe(2); // Aceite + Cadena are recommended
    expect(component.isSelected('d-aceite')).toBe(true);
    expect(component.isSelected('d-bujia')).toBe(false);
  });

  it('redirects demo users to home without loading the plan', () => {
    setup(true);

    component.ngOnInit();

    expect(routerMock.navigate).toHaveBeenCalledWith(['/dashboard/home']);
    expect(maintenanceServiceMock.getDefaultMaintenance).not.toHaveBeenCalled();
  });

  it('blocks advancing to the plan when the form is invalid', () => {
    component.goToPlan();

    expect(swalMock.warning).toHaveBeenCalledWith(
      'Falta un dato',
      'Completa los datos de tu motocicleta para continuar.',
    );
    expect(component.step()).toBe(1);
  });

  it('advances to the plan step when the form is valid', () => {
    fillValidForm();

    component.goToPlan();

    expect(component.step()).toBe(2);
  });

  it('toggles, clears and re-selects the plan', () => {
    component.ngOnInit();

    component.toggle('d-bujia');
    expect(component.isSelected('d-bujia')).toBe(true);
    component.toggle('d-bujia');
    expect(component.isSelected('d-bujia')).toBe(false);

    component.clearSelection();
    expect(component.selectedCount).toBe(0);

    component.selectRecommended();
    expect(component.selectedCount).toBe(2);
  });

  it('creates the motorcycle, follows the selected plan and navigates to the summary', async () => {
    component.ngOnInit();
    fillValidForm();
    motorcyclesServiceMock.addMotorcycle.mockReturnValue(of({ id: 'moto-1' }));
    maintenanceServiceMock.followDefaultMaintenance.mockReturnValue(of({ id: 'um-1' }));

    component.finish();
    await flush();

    expect(motorcyclesServiceMock.addMotorcycle).toHaveBeenCalledWith(
      expect.objectContaining({ name: 'XTZ 150', image: 'default.png' }),
    );
    expect(maintenanceServiceMock.followDefaultMaintenance).toHaveBeenCalledTimes(2);
    expect(maintenanceServiceMock.followDefaultMaintenance).toHaveBeenCalledWith(
      expect.objectContaining({
        motorcycleId: 'moto-1',
        defaultId: 'd-aceite',
        trackingType: 'Km',
        kmInterval: 1500,
        timeIntervalWeeks: 0,
      }),
    );
    expect(maintenanceServiceMock.followDefaultMaintenance).toHaveBeenCalledWith(
      expect.objectContaining({
        defaultId: 'd-cadena',
        trackingType: 'Time',
        kmInterval: 0,
        timeIntervalWeeks: 2,
      }),
    );
    expect(swalMock.success).toHaveBeenCalled();
    expect(routerMock.navigate).toHaveBeenCalledWith(['/dashboard/motorcycles/summary'], {
      state: { motorcycle: { id: 'moto-1' } },
    });
  });

  it('creates the motorcycle only when no maintenance is selected', async () => {
    component.ngOnInit();
    component.clearSelection();
    fillValidForm();
    motorcyclesServiceMock.addMotorcycle.mockReturnValue(of({ id: 'moto-1' }));

    component.finish();
    await flush();

    expect(maintenanceServiceMock.followDefaultMaintenance).not.toHaveBeenCalled();
    expect(routerMock.navigate).toHaveBeenCalledWith(['/dashboard/motorcycles/summary'], {
      state: { motorcycle: { id: 'moto-1' } },
    });
  });

  it('still navigates when one follow call fails, warning about the partial result', async () => {
    component.ngOnInit();
    fillValidForm();
    motorcyclesServiceMock.addMotorcycle.mockReturnValue(of({ id: 'moto-1' }));
    maintenanceServiceMock.followDefaultMaintenance
      .mockReturnValueOnce(of({ id: 'um-1' }))
      .mockReturnValueOnce(throwError(() => new Error('boom')));

    component.finish();
    await flush();

    expect(swalMock.warning).toHaveBeenCalledWith('Casi listo', expect.stringContaining('1'));
    expect(routerMock.navigate).toHaveBeenCalledWith(['/dashboard/motorcycles/summary'], {
      state: { motorcycle: { id: 'moto-1' } },
    });
  });

  it('surfaces an error and does not navigate when the motorcycle cannot be created', async () => {
    component.ngOnInit();
    fillValidForm();
    motorcyclesServiceMock.addMotorcycle.mockReturnValue(
      throwError(() => ({ message: 'No se pudo agregar la motocicleta.' })),
    );

    component.finish();
    await flush();

    expect(swalMock.error).toHaveBeenCalledWith('Error', 'No se pudo agregar la motocicleta.');
    expect(routerMock.navigate).not.toHaveBeenCalled();
  });

  it('reports the plan loading error', () => {
    maintenanceServiceMock.getDefaultMaintenance.mockReturnValue(
      throwError(() => ({ message: 'boom' })),
    );

    component.ngOnInit();

    expect(component.loadingPlan()).toBe(false);
    expect(swalMock.error).toHaveBeenCalledWith('Error', 'boom');
  });
});
