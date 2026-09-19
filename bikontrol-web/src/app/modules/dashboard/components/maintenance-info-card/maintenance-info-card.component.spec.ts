import { MaintenanceInfoCardComponent } from './maintenance-info-card.component';

describe('MaintenanceInfoCardComponent (class)', () => {
  const routerMock = { navigate: vi.fn() } as any;
  const maintenanceServiceMock = {
    followDefaultMaintenance: vi.fn(),
    deleteMaintenance: vi.fn()
  } as any;
  const swalServiceMock = {
    success: vi.fn(),
    error: vi.fn(),
    warning: vi.fn(),
    confirm: vi.fn()
  } as any;
  const httpErrorMock = {
    message: vi.fn((error: any, fallback = 'Error inesperado en el servidor.') => {
      return error?.error?.error || error?.error?.message || error?.message || fallback;
    })
  } as any;

  it('should create', () => {
    const component = new MaintenanceInfoCardComponent(routerMock, maintenanceServiceMock, swalServiceMock, httpErrorMock, { isDemo: () => false } as any);
    expect(component).toBeTruthy();
  });
});
