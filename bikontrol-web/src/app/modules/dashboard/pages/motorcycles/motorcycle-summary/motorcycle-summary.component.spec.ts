import { of, throwError } from 'rxjs';
import { MotorcycleSummaryComponent } from './motorcycle-summary.component';
jest.mock('sweetalert2', () => ({
  __esModule: true,
  default: { fire: jest.fn() }
}));

describe('MotorcycleSummaryComponent (class)', () => {
  const routerMock = {
    getCurrentNavigation: jest.fn().mockReturnValue({ extras: { state: { motorcycle: { id: 'm1' } } } }),
    navigate: jest.fn()
  } as any;

  const activatedRouteMock = {
    snapshot: {
      queryParamMap: {
        get: jest.fn().mockReturnValue(null)
      }
    }
  } as any;

  const maintenanceServiceMock = {
    getUpcomingByMotorcycle: jest.fn().mockReturnValue(of([])),
    getMaintenanceRecordsByMotorcycle: jest.fn().mockReturnValue(of([]))
  } as any;

  const motorcyclesServiceMock = {
    getById: jest.fn().mockReturnValue(throwError(() => new Error('not found'))),
    getCurrentKm: jest.fn().mockReturnValue(of({ km: 1000 })),
    addKmHistory: jest.fn().mockReturnValue(of(undefined)),
    rollbackLastKm: jest.fn().mockReturnValue(of(undefined))
  } as any;

  const swalServiceMock = {
    success: jest.fn().mockReturnValue(Promise.resolve({})),
    error: jest.fn().mockReturnValue(Promise.resolve({})),
    confirm: jest.fn().mockReturnValue(Promise.resolve({ isConfirmed: true }))
  } as any;

  const httpErrorMock = {
    message: jest.fn((error: any, fallback = 'Error inesperado en el servidor.') => {
      return error?.error?.error || error?.error?.message || error?.message || fallback;
    })
  } as any;

  let component: MotorcycleSummaryComponent;

  beforeEach(() => {
    component = new MotorcycleSummaryComponent(
      routerMock,
      activatedRouteMock,
      maintenanceServiceMock,
      motorcyclesServiceMock,
      swalServiceMock,
      httpErrorMock
    );
    component.motorcycle = { id: 'm1' } as any;
    component.currentKm = 1000;
  });

  it('should disable register button when there are no maintenances', () => {
    component.upcomingMaintenances = [];
    expect(component.canRegisterMaintenance).toBe(false);
  });

  it('should open and close edit km modal', () => {
    component.openEditKmModal();
    expect(component.isEditKmModalOpen).toBe(true);
    expect(component.editableKm).toBe(1000);

    component.closeEditKmModal();
    expect(component.isEditKmModalOpen).toBe(false);
  });

  it('should not call addKmHistory when editable km is lower than current', () => {
    component.editableKm = 900;
    component.saveKm();
    expect(motorcyclesServiceMock.addKmHistory).not.toHaveBeenCalled();
    expect(swalServiceMock.error).toHaveBeenCalled();
  });

  it('should call addKmHistory when editable km is valid', () => {
    component.editableKm = 1400;
    component.saveKm();
    expect(motorcyclesServiceMock.addKmHistory).toHaveBeenCalledWith('m1', 1400);
  });

  it('should call rollbackLastKm when user confirms', async () => {
    component.currentKm = 1300;
    component.rollbackLastKm();
    await Promise.resolve();
    expect(motorcyclesServiceMock.rollbackLastKm).toHaveBeenCalledWith('m1', 1300);
  });
});
