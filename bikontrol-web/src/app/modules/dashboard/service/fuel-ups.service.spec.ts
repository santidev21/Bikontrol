import { TestBed } from '@angular/core/testing';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom, of } from 'rxjs';
import { FuelUpsService } from './fuel-ups.service';

describe('FuelUpsService (unit, mocked HttpClient)', () => {
  let service: FuelUpsService;
  let mockHttp: any;

  beforeEach(() => {
    mockHttp = {
      get: vi.fn(),
      post: vi.fn(),
      put: vi.fn(),
      delete: vi.fn(),
    };
    // The service uses inject(), so it must be created inside an injection context.
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [{ provide: HttpClient, useValue: mockHttp }],
    });
    service = TestBed.runInInjectionContext(() => new FuelUpsService());
  });

  afterEach(() => vi.resetAllMocks());

  const payload = {
    motorcycleId: 'moto-1',
    fueledAt: '2026-06-09T00:00:00.000Z',
    odometerKm: 12000,
    litres: 10.5,
    totalCost: 85000,
    isFullTank: true,
    station: 'Terpel',
    notes: null,
  };

  it('should fetch the fuel-ups of a motorcycle', async () => {
    const mock = [{ id: 'f1' }];
    mockHttp.get.mockReturnValue(of(mock));

    const res = await firstValueFrom(service.getByMotorcycle('moto-1'));

    expect(res).toEqual(mock);
    expect(mockHttp.get).toHaveBeenCalledWith(`${service['apiUrl']}/motorcycle/moto-1`);
  });

  it('should get a fuel-up by id', async () => {
    const mock = { id: 'f1' };
    mockHttp.get.mockReturnValue(of(mock));

    const res = await firstValueFrom(service.getById('f1'));

    expect(res).toEqual(mock);
    expect(mockHttp.get).toHaveBeenCalledWith(`${service['apiUrl']}/f1`);
  });

  it('should create a fuel-up with the correct payload', async () => {
    const created = { id: 'f2', ...payload };
    mockHttp.post.mockReturnValue(of(created));

    const res = await firstValueFrom(service.create(payload));

    expect(res).toEqual(created);
    expect(mockHttp.post).toHaveBeenCalledWith(`${service['apiUrl']}`, payload);
  });

  it('should update a fuel-up', async () => {
    const updated = { id: 'f1', ...payload };
    mockHttp.put.mockReturnValue(of(updated));

    const res = await firstValueFrom(service.update('f1', payload));

    expect(res).toEqual(updated);
    expect(mockHttp.put).toHaveBeenCalledWith(`${service['apiUrl']}/f1`, payload);
  });

  it('should delete a fuel-up', async () => {
    mockHttp.delete.mockReturnValue(of(undefined));

    const res = await firstValueFrom(service.delete('f1'));

    expect(res).toBeUndefined();
    expect(mockHttp.delete).toHaveBeenCalledWith(`${service['apiUrl']}/f1`);
  });

  it('should fetch the consumption metrics of a motorcycle', async () => {
    const mock = { motorcycleId: 'moto-1', averageKmPerLitre: 42.5, points: [] };
    mockHttp.get.mockReturnValue(of(mock));

    const res = await firstValueFrom(service.getConsumption('moto-1'));

    expect(res).toEqual(mock);
    expect(mockHttp.get).toHaveBeenCalledWith(`${service['apiUrl']}/motorcycle/moto-1/consumption`);
  });
});
