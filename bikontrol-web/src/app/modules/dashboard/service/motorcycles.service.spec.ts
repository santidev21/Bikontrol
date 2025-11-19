import { of } from 'rxjs';
import { MotorcyclesService } from './motorcycles.service';
import { SaveMotorcycleDTO, Motorcycle } from '../interfaces/motorcycle.interface';

describe('MotorcyclesService (unit, mocked HttpClient)', () => {
  let service: MotorcyclesService;
  let mockHttp: any;

  beforeEach(() => {
    mockHttp = {
      get: jest.fn(),
      post: jest.fn(),
      put: jest.fn(),
      delete: jest.fn()
    };
    service = new MotorcyclesService(mockHttp as any);
  });

  afterEach(() => jest.resetAllMocks());

  it('should fetch my motorcycles', done => {
    const mock: any[] = [{ id: '1', make: 'Yamaha', model: 'X', year: 2020, ownerId: 'u1' }];
    mockHttp.get.mockReturnValue(of(mock));

    service.getMyMotorcycles().subscribe(res => {
      expect(res).toEqual(mock);
      done();
    });
    expect(mockHttp.get).toHaveBeenCalledWith(`${service['apiUrl']}/mine`);
  });

  it('should get motorcycle by id', done => {
    const mock: any = { id: '2', make: 'Honda', model: 'C', year: 2019, ownerId: 'u2' };
    mockHttp.get.mockReturnValue(of(mock));

    service.getById('2').subscribe(res => {
      expect(res).toEqual(mock);
      done();
    });
    expect(mockHttp.get).toHaveBeenCalledWith(`${service['apiUrl']}/2`);
  });

  it('should add a motorcycle', done => {
    const dto: any = { make: 'Kawasaki', model: 'Z', year: 2021 };
    const mock: any = { id: '3', ...dto, ownerId: 'u3' };
    mockHttp.post.mockReturnValue(of(mock));

    service.addMotorcycle(dto).subscribe(res => {
      expect(res).toEqual(mock);
      done();
    });
    expect(mockHttp.post).toHaveBeenCalledWith(`${service['apiUrl']}`, dto);
  });

  it('should update a motorcycle', done => {
    const dto: any = { make: 'Updated', model: 'U', year: 2022 };
    mockHttp.put.mockReturnValue(of(undefined));

    service.updateMotorcycle('4', dto).subscribe(res => {
      expect(res).toBeUndefined();
      done();
    });
    expect(mockHttp.put).toHaveBeenCalledWith(`${service['apiUrl']}/4`, dto);
  });

  it('should delete a motorcycle', done => {
    mockHttp.delete.mockReturnValue(of(undefined));

    service.deleteMotorcycle('5').subscribe(res => {
      expect(res).toBeUndefined();
      done();
    });
    expect(mockHttp.delete).toHaveBeenCalledWith(`${service['apiUrl']}/5`);
  });
});
