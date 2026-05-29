import { SaveMaintenanceComponent } from './save-maintenance.component';

describe('SaveMaintenanceComponent (class)', () => {
  const formBuilderMock = {
    group: jest.fn().mockReturnValue({
      value: {},
      invalid: false,
      markAllAsTouched: jest.fn(),
      get: jest.fn().mockReturnValue({ value: 'km' }),
      patchValue: jest.fn()
    })
  } as any;
  const maintenanceServiceMock = {
    getById: jest.fn(),
    createUserMaintenance: jest.fn(),
    updateMaintenance: jest.fn()
  } as any;
  const routerMock = { navigate: jest.fn() } as any;
  const routeMock = {
    snapshot: {
      paramMap: {
        get: jest.fn()
      }
    },
    paramMap: {
      subscribe: jest.fn()
    }
  } as any;
  const swalMock = {
    warning: jest.fn(),
    error: jest.fn(),
    success: jest.fn()
  } as any;

  it('should create', () => {
    const component = new SaveMaintenanceComponent(formBuilderMock, maintenanceServiceMock, routerMock, routeMock, swalMock);
    expect(component).toBeTruthy();
  });
});
