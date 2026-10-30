import { TestBed } from '@angular/core/testing';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom, of } from 'rxjs';
import { MotorcyclesService } from './motorcycles.service';

describe('MotorcyclesService (unit, mocked HttpClient)', () => {
  let service: MotorcyclesService;
  let mockHttp: any;

  beforeEach(() => {
    mockHttp = {
      get: vi.fn(),
      post: vi.fn(),
      put: vi.fn(),
      delete: vi.fn(),
      request: vi.fn(),
    };
    // The service uses inject(), so it must be created inside an injection context.
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [{ provide: HttpClient, useValue: mockHttp }],
    });
    service = TestBed.runInInjectionContext(() => new MotorcyclesService());
  });

  afterEach(() => vi.resetAllMocks());

  it('should resolve signed photo paths against the API origin and leave other values alone', async () => {
    mockHttp.get.mockReturnValue(
      of([
        { id: '1', image: '/api/media/motorcycles/1?v=a&exp=1&sig=s' },
        { id: '2', image: 'default.png' },
        { id: '3', image: 'data:image/png;base64,AAAA' },
        { id: '4' },
      ]),
    );

    const res = await firstValueFrom(service.getMyMotorcycles());

    expect(res[0].image).toMatch(/^https?:\/\/.+\/api\/media\/motorcycles\/1\?v=a&exp=1&sig=s$/);
    expect(res[1].image).toBe('default.png');
    expect(res[2].image).toBe('data:image/png;base64,AAAA');
    expect(res[3].image).toBeUndefined();
  });

  it('should resolve the photo of a single motorcycle and of a created one', async () => {
    mockHttp.get.mockReturnValue(
      of({ id: '1', image: '/api/media/motorcycles/1?v=a&exp=1&sig=s' }),
    );
    mockHttp.post.mockReturnValue(
      of({ id: '2', image: '/api/media/motorcycles/2?v=b&exp=1&sig=s' }),
    );

    const one = await firstValueFrom(service.getById('1'));
    const created = await firstValueFrom(service.addMotorcycle({} as any));

    expect(one.image).toMatch(/^https?:\/\/.+\/api\/media\/motorcycles\/1/);
    expect(created.image).toMatch(/^https?:\/\/.+\/api\/media\/motorcycles\/2/);
  });

  it('should fetch my motorcycles', async () => {
    const mock: any[] = [
      {
        id: '1',
        name: 'Yamaha',
        brand: 'Yamaha',
        year: 2020,
        nickname: 'X',
        km: 1000,
        displacement: 150,
        plate: 'ABC123',
        isEnabled: true,
      },
    ];
    mockHttp.get.mockReturnValue(of(mock));

    const res = await firstValueFrom(service.getMyMotorcycles());

    expect(res).toEqual(mock);
    expect(mockHttp.get).toHaveBeenCalledWith(`${service['apiUrl']}/mine`);
  });

  it('should get motorcycle by id', async () => {
    const mock: any = {
      id: '2',
      name: 'Honda',
      brand: 'Honda',
      year: 2019,
      nickname: 'C',
      km: 2000,
      displacement: 160,
      plate: 'DEF456',
      isEnabled: true,
    };
    mockHttp.get.mockReturnValue(of(mock));

    const res = await firstValueFrom(service.getById('2'));

    expect(res).toEqual(mock);
    expect(mockHttp.get).toHaveBeenCalledWith(`${service['apiUrl']}/2`);
  });

  it('should add a motorcycle', async () => {
    const dto: any = {
      name: 'Kawasaki',
      brand: 'Kawasaki',
      year: 2021,
      nickname: 'Z',
      km: 10,
      displacement: 300,
      plate: 'GHI789',
    };
    const mock: any = { id: '3', ...dto, isEnabled: true };
    mockHttp.post.mockReturnValue(of(mock));

    const res = await firstValueFrom(service.addMotorcycle(dto));

    expect(res).toEqual(mock);
    expect(mockHttp.post).toHaveBeenCalledWith(`${service['apiUrl']}`, dto);
  });

  it('should update a motorcycle', async () => {
    const dto: any = {
      name: 'Updated',
      brand: 'Updated',
      year: 2022,
      nickname: 'U',
      km: 15,
      displacement: 400,
      plate: 'XYZ999',
    };
    mockHttp.put.mockReturnValue(of(undefined));

    const res = await firstValueFrom(service.updateMotorcycle('4', dto));

    expect(res).toBeUndefined();
    expect(mockHttp.put).toHaveBeenCalledWith(`${service['apiUrl']}/4`, dto);
  });

  it('should delete a motorcycle', async () => {
    mockHttp.delete.mockReturnValue(of(undefined));

    const res = await firstValueFrom(service.deleteMotorcycle('5'));

    expect(res).toBeUndefined();
    expect(mockHttp.delete).toHaveBeenCalledWith(`${service['apiUrl']}/5`);
  });

  it('should get current km with correct endpoint', async () => {
    const mock = { km: 3456 };
    mockHttp.get.mockReturnValue(of(mock));

    const res = await firstValueFrom(service.getCurrentKm('moto-1'));

    expect(res).toEqual(mock);
    expect(mockHttp.get).toHaveBeenCalledWith(`${service['apiUrl']}/moto-1/km/current`);
  });

  it('should add km history with correct endpoint and body', async () => {
    mockHttp.post.mockReturnValue(of(undefined));

    const res = await firstValueFrom(service.addKmHistory('moto-1', 4000));

    expect(res).toBeUndefined();
    expect(mockHttp.post).toHaveBeenCalledWith(`${service['apiUrl']}/moto-1/km-history`, {
      km: 4000,
    });
  });

  it('should rollback last km with delete request and body', async () => {
    mockHttp.request.mockReturnValue(of(undefined));

    const res = await firstValueFrom(service.rollbackLastKm('moto-1', 3500));

    expect(res).toBeUndefined();
    expect(mockHttp.request).toHaveBeenCalledWith(
      'delete',
      `${service['apiUrl']}/moto-1/km-history/last`,
      {
        body: { newKm: 3500 },
      },
    );
  });

  it('should download the maintenance book as a blob', async () => {
    const blob = new Blob(['x']);
    mockHttp.get.mockReturnValue(of(blob));

    const res = await firstValueFrom(service.downloadMaintenanceBook('moto-1', 'pdf'));

    expect(res).toBe(blob);
    expect(mockHttp.get).toHaveBeenCalledWith(
      `${service['apiUrl']}/moto-1/maintenance-book/book.pdf`,
      { responseType: 'blob' },
    );
  });
});
