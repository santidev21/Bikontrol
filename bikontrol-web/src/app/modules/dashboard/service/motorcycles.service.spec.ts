import { of } from "rxjs";
import { MotorcyclesService } from "./motorcycles.service";

describe("MotorcyclesService (unit, mocked HttpClient)", () => {
  let service: MotorcyclesService;
  let mockHttp: any;

  beforeEach(() => {
    mockHttp = {
      get: jest.fn(),
      post: jest.fn(),
      put: jest.fn(),
      delete: jest.fn(),
      request: jest.fn()
    };
    service = new MotorcyclesService(mockHttp as any);
  });

  afterEach(() => jest.resetAllMocks());

  it("should fetch my motorcycles", done => {
    const mock: any[] = [{ id: "1", name: "Yamaha", brand: "Yamaha", year: 2020, nickname: "X", km: 1000, displacement: 150, plate: "ABC123", isEnabled: true }];
    mockHttp.get.mockReturnValue(of(mock));

    service.getMyMotorcycles().subscribe(res => {
      expect(res).toEqual(mock);
      done();
    });
    expect(mockHttp.get).toHaveBeenCalledWith(`${service["apiUrl"]}/mine`);
  });

  it("should get motorcycle by id", done => {
    const mock: any = { id: "2", name: "Honda", brand: "Honda", year: 2019, nickname: "C", km: 2000, displacement: 160, plate: "DEF456", isEnabled: true };
    mockHttp.get.mockReturnValue(of(mock));

    service.getById("2").subscribe(res => {
      expect(res).toEqual(mock);
      done();
    });
    expect(mockHttp.get).toHaveBeenCalledWith(`${service["apiUrl"]}/2`);
  });

  it("should add a motorcycle", done => {
    const dto: any = { name: "Kawasaki", brand: "Kawasaki", year: 2021, nickname: "Z", km: 10, displacement: 300, plate: "GHI789" };
    const mock: any = { id: "3", ...dto, isEnabled: true };
    mockHttp.post.mockReturnValue(of(mock));

    service.addMotorcycle(dto).subscribe(res => {
      expect(res).toEqual(mock);
      done();
    });
    expect(mockHttp.post).toHaveBeenCalledWith(`${service["apiUrl"]}`, dto);
  });

  it("should update a motorcycle", done => {
    const dto: any = { name: "Updated", brand: "Updated", year: 2022, nickname: "U", km: 15, displacement: 400, plate: "XYZ999" };
    mockHttp.put.mockReturnValue(of(undefined));

    service.updateMotorcycle("4", dto).subscribe(res => {
      expect(res).toBeUndefined();
      done();
    });
    expect(mockHttp.put).toHaveBeenCalledWith(`${service["apiUrl"]}/4`, dto);
  });

  it("should delete a motorcycle", done => {
    mockHttp.delete.mockReturnValue(of(undefined));

    service.deleteMotorcycle("5").subscribe(res => {
      expect(res).toBeUndefined();
      done();
    });
    expect(mockHttp.delete).toHaveBeenCalledWith(`${service["apiUrl"]}/5`);
  });

  it("should get current km with correct endpoint", done => {
    const mock = { km: 3456 };
    mockHttp.get.mockReturnValue(of(mock));

    service.getCurrentKm("moto-1").subscribe(res => {
      expect(res).toEqual(mock);
      done();
    });

    expect(mockHttp.get).toHaveBeenCalledWith(`${service["apiUrl"]}/moto-1/km/current`);
  });

  it("should add km history with correct endpoint and body", done => {
    mockHttp.post.mockReturnValue(of(undefined));

    service.addKmHistory("moto-1", 4000).subscribe(res => {
      expect(res).toBeUndefined();
      done();
    });

    expect(mockHttp.post).toHaveBeenCalledWith(`${service["apiUrl"]}/moto-1/km-history`, { km: 4000 });
  });

  it("should rollback last km with delete request and body", done => {
    mockHttp.request.mockReturnValue(of(undefined));

    service.rollbackLastKm("moto-1", 3500).subscribe(res => {
      expect(res).toBeUndefined();
      done();
    });

    expect(mockHttp.request).toHaveBeenCalledWith("delete", `${service["apiUrl"]}/moto-1/km-history/last`, {
      body: { newKm: 3500 }
    });
  });
});
