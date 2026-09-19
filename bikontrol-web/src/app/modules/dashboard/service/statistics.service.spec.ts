import { firstValueFrom, of } from "rxjs";
import { StatisticsService } from "./statistics.service";

describe("StatisticsService (unit, mocked HttpClient)", () => {
  let service: StatisticsService;
  let mockHttp: any;

  beforeEach(() => {
    mockHttp = {
      get: vi.fn(),
      post: vi.fn(),
      put: vi.fn(),
      delete: vi.fn(),
      request: vi.fn()
    };
    service = new StatisticsService(mockHttp as any);
  });

  afterEach(() => vi.resetAllMocks());

  it("should fetch the statistics summary", async () => {
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

    const res = await firstValueFrom(service.getSummary());

    expect(res).toEqual(mock);
    expect(mockHttp.get).toHaveBeenCalledWith(`${service["apiUrl"]}/summary`);
  });
});
