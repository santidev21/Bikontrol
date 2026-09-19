vi.mock('sweetalert2', () => ({
  __esModule: true,
  default: { fire: vi.fn() }
}));

import { of } from 'rxjs';
import { MotorcycleCardComponent } from './motorcycle-card.component';

describe('MotorcycleCardComponent (class)', () => {
  const routerMock = { navigate: vi.fn() } as any;
  const motorcyclesServiceMock = {
    getCurrentKm: vi.fn().mockReturnValue(of({ km: 4567 })),
    deleteMotorcycle: vi.fn().mockReturnValue(of(undefined))
  } as any;
  const swalServiceMock = {
    success: vi.fn().mockReturnValue(Promise.resolve({})),
    error: vi.fn().mockReturnValue(Promise.resolve({})),
    confirm: vi.fn().mockReturnValue(Promise.resolve({ isConfirmed: true }))
  } as any;
  const httpErrorMock = {
    message: vi.fn((error: any, fallback = 'Error inesperado en el servidor.') => {
      return error?.error?.error || error?.error?.message || error?.message || fallback;
    })
  } as any;

  it('should load current km from service and expose displayedKm', () => {
    const component = new MotorcycleCardComponent(routerMock, motorcyclesServiceMock, swalServiceMock, httpErrorMock, { isDemo: () => false } as any);
    component.motorcycle = { id: 'm1', km: 1000, name: 'Moto' } as any;

    component.ngOnInit();

    expect(motorcyclesServiceMock.getCurrentKm).toHaveBeenCalledWith('m1');
    expect(component.displayedKm).toBe(4567);
  });
});
