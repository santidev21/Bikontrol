import { firstValueFrom, of } from "rxjs";
import { MaintenanceService } from "./maintenance.service";

describe("MaintenanceService (unit, mocked HttpClient)", () => {
  let service: MaintenanceService;
  let httpClientMock: any;

  beforeEach(() => {
    httpClientMock = {
      get: vi.fn(),
      post: vi.fn(),
      put: vi.fn(),
      delete: vi.fn()
    };
    service = new MaintenanceService(httpClientMock);
  });

  it("should fetch default maintenance from the correct endpoint", async () => {
    httpClientMock.get.mockReturnValue(of([]));

    const res = await firstValueFrom(service.getDefaultMaintenance());

    expect(res).toEqual([]);
    expect(httpClientMock.get).toHaveBeenCalledWith(`${service["apiUrl"]}/defaults`);
  });

  it("should fetch user maintenance from the correct endpoint", async () => {
    httpClientMock.get.mockReturnValue(of([{ id: "1" }]));

    const res = await firstValueFrom(service.getUserMaintenance());

    expect(res).toEqual([{ id: "1" }]);
    expect(httpClientMock.get).toHaveBeenCalledWith(`${service["apiUrl"]}/mine`);
  });

  it("should fetch maintenance by motorcycle id", async () => {
    httpClientMock.get.mockReturnValue(of([{ id: "2" }]));

    const res = await firstValueFrom(service.getUserMaintenanceByMotorcycle("moto-1"));

    expect(res).toEqual([{ id: "2" }]);
    expect(httpClientMock.get).toHaveBeenCalledWith(`${service["apiUrl"]}/mine/motorcycle/moto-1`);
  });

  it("should get maintenance by id", async () => {
    httpClientMock.get.mockReturnValue(of({ id: "maint-1" }));

    const res = await firstValueFrom(service.getById("maint-1"));

    expect(res).toEqual({ id: "maint-1" });
    expect(httpClientMock.get).toHaveBeenCalledWith(`${service["apiUrl"]}/maint-1`);
  });

  it("should create a user maintenance with the correct payload", async () => {
    const payload = {
      motorcycleId: "moto-1",
      name: "Aceite",
      description: "Cambio de aceite",
      trackingType: "Km",
      kmInterval: 5000
    };
    httpClientMock.post.mockReturnValue(of({ id: "new-maint" }));

    const res = await firstValueFrom(service.createUserMaintenance(payload as any));

    expect(res).toEqual({ id: "new-maint" });
    expect(httpClientMock.post).toHaveBeenCalledWith(`${service["apiUrl"]}/mine`, payload);
  });

  it("should delete a maintenance from the mine endpoint", async () => {
    httpClientMock.delete.mockReturnValue(of(undefined));

    const res = await firstValueFrom(service.deleteMaintenance("maint-1"));

    expect(res).toBeUndefined();
    expect(httpClientMock.delete).toHaveBeenCalledWith(`${service["apiUrl"]}/mine/maint-1`);
  });

  it("should follow default maintenance with the expected payload", async () => {
    const payload = {
      motorcycleId: "moto-1",
      defaultId: "default-1",
      trackingType: "Time",
      kmInterval: 0,
      timeIntervalWeeks: 12
    };
    httpClientMock.post.mockReturnValue(of({ id: "followed" }));

    const res = await firstValueFrom(service.followDefaultMaintenance(payload as any));

    expect(res).toEqual({ id: "followed" });
    expect(httpClientMock.post).toHaveBeenCalledWith(`${service["apiUrl"]}/follow`, payload);
  });

  it("should update an existing maintenance", async () => {
    const payload = {
      name: "Revision",
      description: "Revision general",
      trackingType: "Km"
    };
    httpClientMock.put.mockReturnValue(of(undefined));

    const res = await firstValueFrom(service.updateMaintenance("maint-2", payload as any));

    expect(res).toBeUndefined();
    expect(httpClientMock.put).toHaveBeenCalledWith(`${service["apiUrl"]}/maint-2`, payload);
  });

  it("should register a maintenance record", async () => {
    const payload = {
      motorcycleId: "moto-1",
      userMaintenanceId: "maint-1",
      performedAt: "2026-06-09T00:00:00.000Z",
      performedKm: 1234
    };
    httpClientMock.post.mockReturnValue(of({ id: "record-1" }));

    const res = await firstValueFrom(service.registerMaintenanceRecord(payload as any));

    expect(res).toEqual({ id: "record-1" });
    expect(httpClientMock.post).toHaveBeenCalledWith(`${service["apiUrl"]}/records`, payload);
  });

  it("should fetch maintenance records by motorcycle", async () => {
    httpClientMock.get.mockReturnValue(of([{ id: "record-1" }]));

    const res = await firstValueFrom(service.getMaintenanceRecordsByMotorcycle("moto-1"));

    expect(res).toEqual([{ id: "record-1" }]);
    expect(httpClientMock.get).toHaveBeenCalledWith(`${service["apiUrl"]}/motorcycle/moto-1/records`);
  });

  it("should fetch upcoming maintenance by motorcycle", async () => {
    httpClientMock.get.mockReturnValue(of([{ userMaintenanceId: "maint-1" }]));

    const res = await firstValueFrom(service.getUpcomingByMotorcycle("moto-1"));

    expect(res).toEqual([{ userMaintenanceId: "maint-1" }]);
    expect(httpClientMock.get).toHaveBeenCalledWith(`${service["apiUrl"]}/motorcycle/moto-1/upcoming`);
  });
});
