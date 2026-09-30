vi.mock('sweetalert2', () => ({
  __esModule: true,
  default: { fire: vi.fn() },
}));

import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { MotorcycleCardComponent } from './motorcycle-card.component';
import { MotorcyclesService } from '../../service/motorcycles.service';
import { SwalService } from '../../../../shared/services/swal.service';
import { HttpErrorService } from '../../../../shared/services/http-error.service';
import { AuthService } from '../../../auth/services/auth.service';

describe('MotorcycleCardComponent (class)', () => {
  const routerMock = { navigate: vi.fn() } as any;
  const motorcyclesServiceMock = {
    getCurrentKm: vi.fn().mockReturnValue(of({ km: 4567 })),
    deleteMotorcycle: vi.fn().mockReturnValue(of(undefined)),
  } as any;
  const swalServiceMock = {
    success: vi.fn().mockReturnValue(Promise.resolve({})),
    error: vi.fn().mockReturnValue(Promise.resolve({})),
    confirm: vi.fn().mockReturnValue(Promise.resolve({ isConfirmed: true })),
  } as any;
  const httpErrorMock = {
    message: vi.fn((error: any, fallback = 'Error inesperado en el servidor.') => {
      return error?.error?.error || error?.error?.message || error?.message || fallback;
    }),
  } as any;

  it('should load current km from service and expose displayedKm', () => {
    // The component uses inject(), so it must be created inside an injection context.
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        { provide: Router, useValue: routerMock },
        { provide: MotorcyclesService, useValue: motorcyclesServiceMock },
        { provide: SwalService, useValue: swalServiceMock },
        { provide: HttpErrorService, useValue: httpErrorMock },
        { provide: AuthService, useValue: { isDemo: () => false } },
      ],
    });
    const component = TestBed.runInInjectionContext(() => new MotorcycleCardComponent());
    component.motorcycle = { id: 'm1', km: 1000, name: 'Moto' } as any;

    component.ngOnInit();

    expect(motorcyclesServiceMock.getCurrentKm).toHaveBeenCalledWith('m1');
    expect(component.displayedKm).toBe(4567);
  });
});
