import { of, throwError } from "rxjs";
import { StatisticsComponent } from "./statistics.component";

describe("StatisticsComponent (class)", () => {
  const statisticsServiceMock = {
    getSummary: jest.fn()
  } as any;
  const swalMock = {
    error: jest.fn(),
    success: jest.fn()
  } as any;
  const httpErrorMock = {
    message: jest.fn((error: any, fallback = "Error inesperado en el servidor.") => {
      return error?.error?.error || error?.error?.message || error?.message || fallback;
    })
  } as any;
  const authServiceMock = {
    isDemo: jest.fn().mockReturnValue(false)
  } as any;

  const summaryMock: any = {
    totalMotorcycles: 2,
    totalKm: 20000,
    totalMaintenanceRecords: 4,
    overdueCount: 1,
    dueSoonCount: 1,
    lastActivityAt: "2026-09-10T00:00:00Z",
    health: [
      { bucket: "Vencido", count: 1 },
      { bucket: "Crítico", count: 1 },
      { bucket: "Próximo", count: 1 },
      { bucket: "OK", count: 1 }
    ],
    kmByMotorcycle: [
      { motorcycleId: "m1", name: "Negra", km: 8000 },
      { motorcycleId: "m2", name: "Roja", km: 12000 }
    ],
    recordsByType: [{ name: "Aceite", count: 3 }],
    last6Months: [{ yearMonth: "2026-09", count: 2 }]
  };

  function createComponent() {
    return new StatisticsComponent(statisticsServiceMock, swalMock, httpErrorMock, authServiceMock);
  }

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it("should load the summary on init", () => {
    statisticsServiceMock.getSummary.mockReturnValue(of(summaryMock));
    const component = createComponent();

    component.ngOnInit();

    expect(statisticsServiceMock.getSummary).toHaveBeenCalled();
    expect(component.summary).toEqual(summaryMock);
    expect(component.isLoading).toBe(false);
  });

  it("should show an error when the summary fails to load", () => {
    statisticsServiceMock.getSummary.mockReturnValue(throwError(() => ({ message: "boom" })));
    const component = createComponent();

    component.ngOnInit();

    expect(component.summary).toBeUndefined();
    expect(component.isLoading).toBe(false);
    expect(swalMock.error).toHaveBeenCalledWith("Error", "boom");
  });

  it("should compute bar widths relative to the max", () => {
    const component = createComponent();

    expect(component.barWidth(50, 100)).toBe(50);
    expect(component.barWidth(0, 100)).toBe(0);
    expect(component.barWidth(10, 0)).toBe(0);
  });

  it("should resolve max values from the summary", () => {
    statisticsServiceMock.getSummary.mockReturnValue(of(summaryMock));
    const component = createComponent();
    component.ngOnInit();

    expect(component.maxKm()).toBe(12000);
    expect(component.maxTypeCount()).toBe(3);
    expect(component.maxMonthCount()).toBe(2);
    expect(component.maxHealthCount()).toBe(1);
  });

  it("should map health buckets to colors", () => {
    const component = createComponent();

    expect(component.healthColor("Vencido")).toBe("#ef4444");
    expect(component.healthColor("OK")).toBe("#10b981");
    expect(component.healthColor("Desconocido")).toBe("#6b7280");
  });

  it("should format month labels in spanish", () => {
    const component = createComponent();

    expect(component.monthLabel("2026-09")).toBe("sep");
    expect(component.monthLabel("2026-01")).toBe("ene");
    expect(component.monthLabel("invalid")).toBe("invalid");
  });
});
