import { of } from "rxjs";
import { MaintenanceService } from "./maintenance.service";

describe("MaintenanceService (unit, mocked HttpClient)", () => {
  let service: MaintenanceService;
  let httpClientMock: any;

  beforeEach(() => {
    httpClientMock = {
      get: jest.fn(),
      post: jest.fn(),
      put: jest.fn(),
      delete: jest.fn()
    };
    service = new MaintenanceService(httpClientMock);
  });

  it("should fetch default maintenance from the correct endpoint", done => {
    httpClientMock.get.mockReturnValue(of([]));

    service.getDefaultMaintenance().subscribe(res => {
      expect(res).toEqual([]);
      done();
    });

    expect(httpClientMock.get).toHaveBeenCalledWith(`${service["apiUrl"]}/defaults`);
  });

  it("should fetch user maintenance from the correct endpoint", done => {
    httpClientMock.get.mockReturnValue(of([{ id: "1" }]));

    service.getUserMaintenance().subscribe(res => {
      expect(res).toEqual([{ id: "1" }]);
      done();
    });

    expect(httpClientMock.get).toHaveBeenCalledWith(`${service["apiUrl"]}/mine`);
  });

  it("should fetch maintenance by motorcycle id", done => {
    httpClientMock.get.mockReturnValue(of([{ id: "2" }]));

    service.getUserMaintenanceByMotorcycle("moto-1").subscribe(res => {
      expect(res).toEqual([{ id: "2" }]);
      done();
    });

    expect(httpClientMock.get).toHaveBeenCalledWith(`${service["apiUrl"]}/mine/motorcycle/moto-1`);
  });

  it("should get maintenance by id", done => {
    httpClientMock.get.mockReturnValue(of({ id: "maint-1" }));

    service.getById("maint-1").subscribe(res => {
      expect(res).toEqual({ id: "maint-1" });
      done();
    });

    expect(httpClientMock.get).toHaveBeenCalledWith(`${service["apiUrl"]}/maint-1`);
  });

  it("should create a user maintenance with the correct payload", done => {
    const payload = {
      motorcycleId: "moto-1",
      name: "Aceite",
      description: "Cambio de aceite",
      trackingType: "Km",
      kmInterval: 5000
    };
    httpClientMock.post.mockReturnValue(of({ id: "new-maint" }));

    service.createUserMaintenance(payload as any).subscribe(res => {
      expect(res).toEqual({ id: "new-maint" });
      done();
    });

    expect(httpClientMock.post).toHaveBeenCalledWith(`${service["apiUrl"]}/mine`, payload);
  });

  it("should delete a maintenance from the mine endpoint", done => {
    httpClientMock.delete.mockReturnValue(of(undefined));

    service.deleteMaintenance("maint-1").subscribe(res => {
      expect(res).toBeUndefined();
      done();
    });

    expect(httpClientMock.delete).toHaveBeenCalledWith(`${service["apiUrl"]}/mine/maint-1`);
  });

  it("should follow default maintenance with the expected payload", done => {
    const payload = {
      motorcycleId: "moto-1",
      defaultId: "default-1",
      trackingType: "Time",
      kmInterval: 0,
      timeIntervalWeeks: 12
    };
    httpClientMock.post.mockReturnValue(of({ id: "followed" }));

    service.followDefaultMaintenance(payload as any).subscribe(res => {
      expect(res).toEqual({ id: "followed" });
      done();
    });

    expect(httpClientMock.post).toHaveBeenCalledWith(`${service["apiUrl"]}/follow`, payload);
  });

  it("should update an existing maintenance", done => {
    const payload = {
      name: "Revision",
      description: "Revision general",
      trackingType: "Km"
    };
    httpClientMock.put.mockReturnValue(of(undefined));

    service.updateMaintenance("maint-2", payload as any).subscribe(res => {
      expect(res).toBeUndefined();
      done();
    });

    expect(httpClientMock.put).toHaveBeenCalledWith(`${service["apiUrl"]}/maint-2`, payload);
  });

  it("should register a maintenance record", done => {
    const payload = {
      motorcycleId: "moto-1",
      userMaintenanceId: "maint-1",
      performedAt: "2026-06-09T00:00:00.000Z",
      performedKm: 1234
    };
    httpClientMock.post.mockReturnValue(of({ id: "record-1" }));

    service.registerMaintenanceRecord(payload as any).subscribe(res => {
      expect(res).toEqual({ id: "record-1" });
      done();
    });

    expect(httpClientMock.post).toHaveBeenCalledWith(`${service["apiUrl"]}/records`, payload);
  });

  it("should fetch maintenance records by motorcycle", done => {
    httpClientMock.get.mockReturnValue(of([{ id: "record-1" }]));

    service.getMaintenanceRecordsByMotorcycle("moto-1").subscribe(res => {
      expect(res).toEqual([{ id: "record-1" }]);
      done();
    });

    expect(httpClientMock.get).toHaveBeenCalledWith(`${service["apiUrl"]}/motorcycle/moto-1/records`);
  });

  it("should fetch upcoming maintenance by motorcycle", done => {
    httpClientMock.get.mockReturnValue(of([{ userMaintenanceId: "maint-1" }]));

    service.getUpcomingByMotorcycle("moto-1").subscribe(res => {
      expect(res).toEqual([{ userMaintenanceId: "maint-1" }]);
      done();
    });

    expect(httpClientMock.get).toHaveBeenCalledWith(`${service["apiUrl"]}/motorcycle/moto-1/upcoming`);
  });
});
