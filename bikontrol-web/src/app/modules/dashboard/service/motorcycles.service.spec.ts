import { firstValueFrom, of } from "rxjs";
import { MotorcyclesService } from "./motorcycles.service";

describe("MotorcyclesService (unit, mocked HttpClient)", () => {
  let service: MotorcyclesService;
  let mockHttp: any;

  beforeEach(() => {
    mockHttp = {
      get: vi.fn(),
      post: vi.fn(),
      put: vi.fn(),
      delete: vi.fn(),
      request: vi.fn()
    };
    service = new MotorcyclesService(mockHttp as any);
  });

  afterEach(() => vi.resetAllMocks());

  it("should fetch my motorcycles", async () => {
    const mock: any[] = [{ id: "1", name: "Yamaha", brand: "Yamaha", year: 2020, nickname: "X", km: 1000, displacement: 150, plate: "ABC123", isEnabled: true }];
    mockHttp.get.mockReturnValue(of(mock));

    const res = await firstValueFrom(service.getMyMotorcycles());

    expect(res).toEqual(mock);
    expect(mockHttp.get).toHaveBeenCalledWith(`${service["apiUrl"]}/mine`);
  });

  it("should get motorcycle by id", async () => {
    const mock: any = { id: "2", name: "Honda", brand: "Honda", year: 2019, nickname: "C", km: 2000, displacement: 160, plate: "DEF456", isEnabled: true };
    mockHttp.get.mockReturnValue(of(mock));

    const res = await firstValueFrom(service.getById("2"));

    expect(res).toEqual(mock);
    expect(mockHttp.get).toHaveBeenCalledWith(`${service["apiUrl"]}/2`);
  });

  it("should add a motorcycle", async () => {
    const dto: any = { name: "Kawasaki", brand: "Kawasaki", year: 2021, nickname: "Z", km: 10, displacement: 300, plate: "GHI789" };
    const mock: any = { id: "3", ...dto, isEnabled: true };
    mockHttp.post.mockReturnValue(of(mock));

    const res = await firstValueFrom(service.addMotorcycle(dto));

    expect(res).toEqual(mock);
    expect(mockHttp.post).toHaveBeenCalledWith(`${service["apiUrl"]}`, dto);
  });

  it("should update a motorcycle", async () => {
    const dto: any = { name: "Updated", brand: "Updated", year: 2022, nickname: "U", km: 15, displacement: 400, plate: "XYZ999" };
    mockHttp.put.mockReturnValue(of(undefined));

    const res = await firstValueFrom(service.updateMotorcycle("4", dto));

    expect(res).toBeUndefined();
    expect(mockHttp.put).toHaveBeenCalledWith(`${service["apiUrl"]}/4`, dto);
  });

  it("should delete a motorcycle", async () => {
    mockHttp.delete.mockReturnValue(of(undefined));

    const res = await firstValueFrom(service.deleteMotorcycle("5"));

    expect(res).toBeUndefined();
    expect(mockHttp.delete).toHaveBeenCalledWith(`${service["apiUrl"]}/5`);
  });

  it("should get current km with correct endpoint", async () => {
    const mock = { km: 3456 };
    mockHttp.get.mockReturnValue(of(mock));

    const res = await firstValueFrom(service.getCurrentKm("moto-1"));

    expect(res).toEqual(mock);
    expect(mockHttp.get).toHaveBeenCalledWith(`${service["apiUrl"]}/moto-1/km/current`);
  });

  it("should add km history with correct endpoint and body", async () => {
    mockHttp.post.mockReturnValue(of(undefined));

    const res = await firstValueFrom(service.addKmHistory("moto-1", 4000));

    expect(res).toBeUndefined();
    expect(mockHttp.post).toHaveBeenCalledWith(`${service["apiUrl"]}/moto-1/km-history`, { km: 4000 });
  });

  it("should rollback last km with delete request and body", async () => {
    mockHttp.request.mockReturnValue(of(undefined));

    const res = await firstValueFrom(service.rollbackLastKm("moto-1", 3500));

    expect(res).toBeUndefined();
    expect(mockHttp.request).toHaveBeenCalledWith("delete", `${service["apiUrl"]}/moto-1/km-history/last`, {
      body: { newKm: 3500 }
    });
  });
});
