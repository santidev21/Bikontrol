import { of } from "rxjs";
import { StatisticsService } from "./statistics.service";

describe("StatisticsService (unit, mocked HttpClient)", () => {
  let service: StatisticsService;
  let mockHttp: any;

  beforeEach(() => {
    mockHttp = {
      get: jest.fn(),
      post: jest.fn(),
      put: jest.fn(),
      delete: jest.fn(),
      request: jest.fn()
    };
    service = new StatisticsService(mockHttp as any);
  });

  afterEach(() => jest.resetAllMocks());

  it("should fetch the statistics summary", done => {
    const mock: any = {
      totalMotorcycles: 2,
      totalKm: 20000,
      totalMaintenanceRecords: 4,
      overdueCount: 1,
      dueSoonCount: 1,
      lastActivityAt: "2026-09-10T00:00:00Z",
      health: [{ bucket: "Vencido", count: 1 }],
      kmByMotorcycle: [],
      recordsByType: [],
      last6Months: []
    };
    mockHttp.get.mockReturnValue(of(mock));

    service.getSummary().subscribe(res => {
      expect(res).toEqual(mock);
      done();
    });
    expect(mockHttp.get).toHaveBeenCalledWith(`${service["apiUrl"]}/summary`);
  });
});
