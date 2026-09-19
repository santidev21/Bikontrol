import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../../auth/services/auth.service';
import { HttpErrorService } from '../../../../../shared/services/http-error.service';
import { SwalService } from '../../../../../shared/services/swal.service';
import { MaintenanceService } from '../../../service/maintenance.service';
import { MaintenancePageComponent } from './maintenance-page.component';

function fakeResource<T>(initial?: T) {
  const value = signal<T | undefined>(initial);
  return {
    value,
    hasValue: () => value() !== undefined,
    error: signal<Error | undefined>(undefined),
    isLoading: signal(false),
    status: signal('idle'),
    reload: vi.fn()
  } as any;
}

describe('MaintenancePageComponent', () => {
  let maintenanceServiceMock: any;
  let routeMock: any;
  let routerMock: any;
  let swalMock: any;

  beforeEach(() => {
    maintenanceServiceMock = {
      getUserMaintenanceByMotorcycleResource: vi.fn(() => fakeResource()),
      getDefaultsResource: vi.fn(() => fakeResource())
    };
    routeMock = { snapshot: { paramMap: { get: vi.fn() } } };
    routerMock = { navigate: vi.fn() };
    swalMock = { warning: vi.fn(), error: vi.fn(), success: vi.fn() };

    TestBed.configureTestingModule({
      imports: [MaintenancePageComponent],
      providers: [
        { provide: MaintenanceService, useValue: maintenanceServiceMock },
        { provide: ActivatedRoute, useValue: routeMock },
        { provide: Router, useValue: routerMock },
        { provide: SwalService, useValue: swalMock },
        { provide: HttpErrorService, useValue: { message: (err: any, fallback: string) => err?.message ?? fallback } },
        { provide: AuthService, useValue: { isDemo: () => false } }
      ]
    });
  });

  function create() {
    return TestBed.createComponent(MaintenancePageComponent).componentInstance;
  }

  it('redirects to home when no motorcycle id exists', () => {
    routeMock.snapshot.paramMap.get.mockReturnValue(null);
    const component = create();

    component.ngOnInit();

    expect(swalMock.warning).toHaveBeenCalledWith(
      'Contexto requerido',
      'Primero selecciona una motocicleta para gestionar mantenimientos.'
    );
    expect(routerMock.navigate).toHaveBeenCalledWith(['/dashboard/home']);
  });

  it('exposes the user and default maintenance from the resources', () => {
    routeMock.snapshot.paramMap.get.mockReturnValue('moto-1');
    maintenanceServiceMock.getUserMaintenanceByMotorcycleResource.mockReturnValue(fakeResource([{ id: '1' }]));
    maintenanceServiceMock.getDefaultsResource.mockReturnValue(fakeResource([{ id: '2' }]));

    const component = create();
    component.ngOnInit();

    expect(component.motorcycleId()).toBe('moto-1');
    expect(component.userMaintenance()).toEqual([{ id: '1' }]);
    expect(component.defaultMaintenance()).toEqual([{ id: '2' }]);
  });

  it('navigates to the add maintenance route with the current motorcycle id', () => {
    const component = create();
    component.motorcycleId.set('moto-1');

    component.goToAddMaintenance();

    expect(routerMock.navigate).toHaveBeenCalledWith(['/dashboard/motorcycles', 'moto-1', 'maintenance/add']);
  });
});
