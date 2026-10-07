import { TestBed } from '@angular/core/testing';
import { FormBuilder } from '@angular/forms';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';
import { RegisterMaintenanceRecordComponent } from './register-maintenance-record.component';
import { MaintenanceService } from '../../../service/maintenance.service';
import { MotorcyclesService } from '../../../service/motorcycles.service';
import { SwalService } from '../../../../../shared/services/swal.service';
import { HttpErrorService } from '../../../../../shared/services/http-error.service';

const kmMaintenance = (id = 'maint-1', name = 'Aceite') => ({
  id,
  motorcycleId: 'moto-1',
  name,
  trackingType: 'Km' as const,
  isEnabled: true,
  isSystem: false,
});

const timeMaintenance = (id = 'maint-2', name = 'Refrigerante') => ({
  id,
  motorcycleId: 'moto-1',
  name,
  trackingType: 'Time' as const,
  isEnabled: true,
  isSystem: false,
});

describe('RegisterMaintenanceRecordComponent', () => {
  let component: RegisterMaintenanceRecordComponent;
  let maintenanceServiceMock: any;
  let motorcyclesServiceMock: any;
  let routerMock: any;
  let routeParamMap$: Subject<any>;
  let routeSnapshot: any;
  let swalMock: any;
  let httpErrorMock: any;

  beforeEach(() => {
    routeParamMap$ = new Subject<any>();
    routeSnapshot = { queryParamMap: convertToParamMap({}) };
    maintenanceServiceMock = {
      getUserMaintenanceByMotorcycle: vi.fn(() => of([])),
      getMaintenanceRecordsByMotorcycle: vi.fn(() => of([])),
      registerMaintenanceRecord: vi.fn(),
    };
    motorcyclesServiceMock = { getCurrentKm: vi.fn(() => of({ km: 2300 })) };
    routerMock = { navigate: vi.fn() };
    swalMock = {
      error: vi.fn(),
      warning: vi.fn().mockResolvedValue(true),
      success: vi.fn().mockResolvedValue(true),
    };
    httpErrorMock = {
      message: vi.fn(
        (error: any, fallback = 'Error inesperado en el servidor.') =>
          error?.error?.error || error?.error?.message || error?.message || fallback,
      ),
    };

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        { provide: FormBuilder, useValue: new FormBuilder() },
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: routeParamMap$.asObservable(),
            get snapshot() {
              return routeSnapshot;
            },
          },
        },
        { provide: Router, useValue: routerMock },
        { provide: MaintenanceService, useValue: maintenanceServiceMock },
        { provide: MotorcyclesService, useValue: motorcyclesServiceMock },
        { provide: SwalService, useValue: swalMock },
        { provide: HttpErrorService, useValue: httpErrorMock },
      ],
    });
    component = TestBed.runInInjectionContext(() => new RegisterMaintenanceRecordComponent());
  });

  const select = (id: string) => component.toggleSelection(id);
  const today = () => new Date().toISOString().split('T')[0];

  it('should redirect to home when no motorcycle id is present', () => {
    component.ngOnInit();
    routeParamMap$.next(convertToParamMap({}));

    expect(routerMock.navigate).toHaveBeenCalledWith(['/dashboard/home']);
  });

  it('should load current km and the user maintenances', () => {
    maintenanceServiceMock.getUserMaintenanceByMotorcycle.mockReturnValue(of([kmMaintenance()]));

    component.ngOnInit();
    routeParamMap$.next(convertToParamMap({ motorcycleId: 'moto-1' }));

    expect(component.motorcycleId()).toBe('moto-1');
    expect(motorcyclesServiceMock.getCurrentKm).toHaveBeenCalledWith('moto-1');
    expect(component.currentKm()).toBe(2300);
    expect(component.maintenances().length).toBe(1);
  });

  it('requires the km field once a km-tracked maintenance is selected', () => {
    maintenanceServiceMock.getUserMaintenanceByMotorcycle.mockReturnValue(of([kmMaintenance()]));
    component.ngOnInit();
    routeParamMap$.next(convertToParamMap({ motorcycleId: 'moto-1' }));

    expect(component.anyKmSelected()).toBe(false);

    select('maint-1');

    expect(component.anyKmSelected()).toBe(true);
    expect(component.form.get('performedKm')?.value).toBe(2300);
    expect(component.form.get('performedKm')?.hasError('required')).toBe(false);
  });

  it('clears the km field when only time-tracked maintenances are selected', () => {
    maintenanceServiceMock.getUserMaintenanceByMotorcycle.mockReturnValue(of([timeMaintenance()]));
    component.ngOnInit();
    routeParamMap$.next(convertToParamMap({ motorcycleId: 'moto-1' }));

    select('maint-2');

    expect(component.anyKmSelected()).toBe(false);
    expect(component.form.get('performedKm')?.value).toBeNull();
  });

  it('warns when nothing is selected', () => {
    component.ngOnInit();
    routeParamMap$.next(convertToParamMap({ motorcycleId: 'moto-1' }));

    component.onSubmit();

    expect(swalMock.warning).toHaveBeenCalledWith('Error', 'Selecciona al menos un mantenimiento.');
    expect(maintenanceServiceMock.registerMaintenanceRecord).not.toHaveBeenCalled();
  });

  it('warns when the date is in the future', () => {
    maintenanceServiceMock.getUserMaintenanceByMotorcycle.mockReturnValue(of([timeMaintenance()]));
    component.ngOnInit();
    routeParamMap$.next(convertToParamMap({ motorcycleId: 'moto-1' }));
    select('maint-2');
    component.form.patchValue({ performedAt: '2999-01-01' });

    component.onSubmit();

    expect(swalMock.warning).toHaveBeenCalledWith(
      'Error',
      'No puedes agregar mantenimientos posteriores al dia de hoy',
    );
    expect(maintenanceServiceMock.registerMaintenanceRecord).not.toHaveBeenCalled();
  });

  it('warns when the shared km is lower than a selected item last record', () => {
    maintenanceServiceMock.getUserMaintenanceByMotorcycle.mockReturnValue(of([kmMaintenance()]));
    maintenanceServiceMock.getMaintenanceRecordsByMotorcycle.mockReturnValue(
      of([{ userMaintenanceId: 'maint-1', performedKm: 3000 }]),
    );
    component.ngOnInit();
    routeParamMap$.next(convertToParamMap({ motorcycleId: 'moto-1' }));
    select('maint-1');
    component.form.patchValue({ performedAt: today(), performedKm: 2100 });

    component.onSubmit();

    expect(swalMock.warning).toHaveBeenCalledWith(
      'Error',
      'No puedes agregar mantenimiento anterior al ultimo',
    );
    expect(maintenanceServiceMock.registerMaintenanceRecord).not.toHaveBeenCalled();
  });

  it('registers every selected maintenance with the shared date and km, then navigates', async () => {
    maintenanceServiceMock.getUserMaintenanceByMotorcycle.mockReturnValue(
      of([kmMaintenance('maint-1', 'Aceite'), kmMaintenance('maint-2', 'Filtro')]),
    );
    maintenanceServiceMock.registerMaintenanceRecord.mockReturnValue(of({ id: 'record-1' }));
    component.ngOnInit();
    routeParamMap$.next(convertToParamMap({ motorcycleId: 'moto-1' }));
    select('maint-1');
    select('maint-2');
    component.form.patchValue({ performedAt: today(), performedKm: 2300 });

    component.onSubmit();
    await Promise.resolve();
    await Promise.resolve();

    expect(maintenanceServiceMock.registerMaintenanceRecord).toHaveBeenCalledTimes(2);
    expect(maintenanceServiceMock.registerMaintenanceRecord).toHaveBeenCalledWith({
      motorcycleId: 'moto-1',
      userMaintenanceId: 'maint-1',
      performedAt: expect.any(String),
      performedKm: 2300,
      cost: null,
    });
    expect(routerMock.navigate).toHaveBeenCalledWith(['/dashboard/motorcycles/summary'], {
      queryParams: { motorcycleId: 'moto-1' },
    });
  });

  it('sends performedKm null for time-tracked items and includes per-item cost', async () => {
    maintenanceServiceMock.getUserMaintenanceByMotorcycle.mockReturnValue(
      of([kmMaintenance('maint-1'), timeMaintenance('maint-2')]),
    );
    maintenanceServiceMock.registerMaintenanceRecord.mockReturnValue(of({ id: 'record-1' }));
    component.ngOnInit();
    routeParamMap$.next(convertToParamMap({ motorcycleId: 'moto-1' }));
    select('maint-1');
    select('maint-2');
    component.setCost('maint-2', { target: { value: '55.5' } } as any);
    component.form.patchValue({ performedAt: today(), performedKm: 2300 });

    component.onSubmit();
    await Promise.resolve();
    await Promise.resolve();

    expect(maintenanceServiceMock.registerMaintenanceRecord).toHaveBeenCalledWith(
      expect.objectContaining({ userMaintenanceId: 'maint-2', performedKm: null, cost: 55.5 }),
    );
  });

  it('reports a partial failure without losing the successful ones', async () => {
    maintenanceServiceMock.getUserMaintenanceByMotorcycle.mockReturnValue(
      of([kmMaintenance('maint-1', 'Aceite'), kmMaintenance('maint-2', 'Filtro')]),
    );
    maintenanceServiceMock.registerMaintenanceRecord.mockImplementation((payload: any) =>
      payload.userMaintenanceId === 'maint-2'
        ? throwError(() => ({ error: { error: 'boom' } }))
        : of({ id: 'record-1' }),
    );
    component.ngOnInit();
    routeParamMap$.next(convertToParamMap({ motorcycleId: 'moto-1' }));
    select('maint-1');
    select('maint-2');
    component.form.patchValue({ performedAt: today(), performedKm: 2300 });

    component.onSubmit();
    await Promise.resolve();
    await Promise.resolve();
    await Promise.resolve();

    expect(swalMock.warning).toHaveBeenCalledWith(
      'Registro parcial',
      'No se pudieron registrar: Filtro. El resto sí se guardó.',
    );
    expect(routerMock.navigate).toHaveBeenCalledWith(['/dashboard/motorcycles/summary'], {
      queryParams: { motorcycleId: 'moto-1' },
    });
  });

  it('surfaces the backend error when nothing could be registered', () => {
    maintenanceServiceMock.getUserMaintenanceByMotorcycle.mockReturnValue(of([kmMaintenance()]));
    maintenanceServiceMock.registerMaintenanceRecord.mockReturnValue(
      throwError(() => ({ error: { error: 'No se pudo registrar el mantenimiento.' } })),
    );
    component.ngOnInit();
    routeParamMap$.next(convertToParamMap({ motorcycleId: 'moto-1' }));
    select('maint-1');
    component.form.patchValue({ performedAt: today(), performedKm: 2300 });

    component.onSubmit();

    expect(swalMock.error).toHaveBeenCalledWith('Error', 'No se pudo registrar el mantenimiento.');
  });

  it('preselects the maintenance passed through the URL', () => {
    routeSnapshot = { queryParamMap: convertToParamMap({ userMaintenanceId: 'maint-1' }) };
    maintenanceServiceMock.getUserMaintenanceByMotorcycle.mockReturnValue(of([kmMaintenance()]));

    component.ngOnInit();
    routeParamMap$.next(convertToParamMap({ motorcycleId: 'moto-1' }));

    expect(component.selectedIds()).toEqual(['maint-1']);
    expect(component.anyKmSelected()).toBe(true);
    expect(component.form.get('performedKm')?.value).toBe(2300);
  });

  it('ignores an unknown maintenance id in the URL', () => {
    routeSnapshot = { queryParamMap: convertToParamMap({ userMaintenanceId: 'nope' }) };
    maintenanceServiceMock.getUserMaintenanceByMotorcycle.mockReturnValue(of([kmMaintenance()]));

    component.ngOnInit();
    routeParamMap$.next(convertToParamMap({ motorcycleId: 'moto-1' }));

    expect(component.selectedIds()).toEqual([]);
  });
});
