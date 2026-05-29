import { MaintenancePageComponent } from './maintenance-page.component';

describe('MaintenancePageComponent (class)', () => {
  const maintenanceServiceMock = {
    getUserMaintenanceByMotorcycle: jest.fn(),
    getDefaultMaintenance: jest.fn()
  } as any;
  const routeMock = {
    snapshot: {
      paramMap: {
        get: jest.fn()
      }
    }
  } as any;
  const routerMock = { navigate: jest.fn() } as any;
  const swalMock = {
    warning: jest.fn(),
    error: jest.fn(),
    success: jest.fn()
  } as any;

  it('should create', () => {
    const component = new MaintenancePageComponent(maintenanceServiceMock, routeMock, routerMock, swalMock);
    expect(component).toBeTruthy();
  });
});
