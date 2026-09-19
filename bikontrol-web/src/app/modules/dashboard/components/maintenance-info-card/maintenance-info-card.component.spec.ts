import { MaintenanceInfoCardComponent } from './maintenance-info-card.component';

describe('MaintenanceInfoCardComponent (class)', () => {
  const routerMock = { navigate: jest.fn() } as any;
  const maintenanceServiceMock = {
    followDefaultMaintenance: jest.fn(),
    deleteMaintenance: jest.fn()
  } as any;
  const swalServiceMock = {
    success: jest.fn(),
    error: jest.fn(),
    warning: jest.fn(),
    confirm: jest.fn()
  } as any;
  const httpErrorMock = {
    message: jest.fn((error: any, fallback = 'Error inesperado en el servidor.') => {
      return error?.error?.error || error?.error?.message || error?.message || fallback;
    })
  } as any;

  it('should create', () => {
    const component = new MaintenanceInfoCardComponent(routerMock, maintenanceServiceMock, swalServiceMock, httpErrorMock, { isDemo: () => false } as any);
    expect(component).toBeTruthy();
  });
});
