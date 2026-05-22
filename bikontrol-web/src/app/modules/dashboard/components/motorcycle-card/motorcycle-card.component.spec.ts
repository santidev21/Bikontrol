jest.mock('sweetalert2', () => ({
  __esModule: true,
  default: { fire: jest.fn() }
}));

import { of } from 'rxjs';
import { MotorcycleCardComponent } from './motorcycle-card.component';

describe('MotorcycleCardComponent (class)', () => {
  const routerMock = { navigate: jest.fn() } as any;
  const motorcyclesServiceMock = {
    getCurrentKm: jest.fn().mockReturnValue(of({ km: 4567 })),
    deleteMotorcycle: jest.fn().mockReturnValue(of(undefined))
  } as any;
  const swalServiceMock = {
    success: jest.fn().mockReturnValue(Promise.resolve({})),
    error: jest.fn().mockReturnValue(Promise.resolve({})),
    confirm: jest.fn().mockReturnValue(Promise.resolve({ isConfirmed: true }))
  } as any;

  it('should load current km from service and expose displayedKm', () => {
    const component = new MotorcycleCardComponent(routerMock, motorcyclesServiceMock, swalServiceMock);
    component.motorcycle = { id: 'm1', km: 1000, name: 'Moto' };

    component.ngOnInit();

    expect(motorcyclesServiceMock.getCurrentKm).toHaveBeenCalledWith('m1');
    expect(component.displayedKm).toBe(4567);
  });
});

