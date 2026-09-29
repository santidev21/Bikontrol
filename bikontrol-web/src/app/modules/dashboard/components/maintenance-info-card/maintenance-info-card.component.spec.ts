import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { MaintenanceInfoCardComponent } from './maintenance-info-card.component';
import { MaintenanceService } from '../../service/maintenance.service';
import { SwalService } from '../../../../shared/services/swal.service';
import { HttpErrorService } from '../../../../shared/services/http-error.service';
import { AuthService } from '../../../auth/services/auth.service';

describe('MaintenanceInfoCardComponent (class)', () => {
  const routerMock = { navigate: vi.fn() } as any;
  const maintenanceServiceMock = {
    followDefaultMaintenance: vi.fn(),
    deleteMaintenance: vi.fn(),
  } as any;
  const swalServiceMock = {
    success: vi.fn(),
    error: vi.fn(),
    warning: vi.fn(),
    confirm: vi.fn(),
  } as any;
  const httpErrorMock = {
    message: vi.fn((error: any, fallback = 'Error inesperado en el servidor.') => {
      return error?.error?.error || error?.error?.message || error?.message || fallback;
    }),
  } as any;

  it('should create', () => {
    // The component uses inject(), so it must be created inside an injection context.
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        { provide: Router, useValue: routerMock },
        { provide: MaintenanceService, useValue: maintenanceServiceMock },
        { provide: SwalService, useValue: swalServiceMock },
        { provide: HttpErrorService, useValue: httpErrorMock },
        { provide: AuthService, useValue: { isDemo: () => false } },
      ],
    });
    const component = TestBed.runInInjectionContext(() => new MaintenanceInfoCardComponent());
    expect(component).toBeTruthy();
  });
});
