import { of } from "rxjs";
import { MaintenancePageComponent } from "./maintenance-page.component";

describe("MaintenancePageComponent (class)", () => {
  const maintenanceServiceMock = {
    getUserMaintenanceByMotorcycle: jest.fn(),
    getDefaultMaintenance: jest.fn()
  } as any;
  const routeMock = {
    snapshot: {
      paramMap: {
        get: jest.fn()
      }
    }
  } as any;
  const routerMock = { navigate: jest.fn() } as any;
  const swalMock = {
    warning: jest.fn(),
    error: jest.fn(),
    success: jest.fn()
  } as any;
  const httpErrorMock = {
    message: jest.fn((error: any, fallback = "Error inesperado en el servidor.") => {
      return error?.error?.error || error?.error?.message || error?.message || fallback;
    })
  } as any;

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it("should redirect to home when no motorcycle id exists", () => {
    routeMock.snapshot.paramMap.get.mockReturnValue(null);
    const component = new MaintenancePageComponent(maintenanceServiceMock, routeMock, routerMock, swalMock, httpErrorMock);

    component.ngOnInit();

    expect(swalMock.warning).toHaveBeenCalledWith(
      "Contexto requerido",
      "Primero selecciona una motocicleta para gestionar mantenimientos."
    );
    expect(routerMock.navigate).toHaveBeenCalledWith(["/dashboard/home"]);
  });

  it("should load user and default maintenance when a motorcycle id exists", () => {
    routeMock.snapshot.paramMap.get.mockReturnValue("moto-1");
    maintenanceServiceMock.getUserMaintenanceByMotorcycle.mockReturnValue(of([{ id: "1" }]));
    maintenanceServiceMock.getDefaultMaintenance.mockReturnValue(of([{ id: "2" }]));
    const component = new MaintenancePageComponent(maintenanceServiceMock, routeMock, routerMock, swalMock, httpErrorMock);

    component.ngOnInit();

    expect(component.motorcycleId).toBe("moto-1");
    expect(component.userMaintenance).toEqual([{ id: "1" }]);
    expect(component.defaultMaintenance).toEqual([{ id: "2" }]);
  });

  it("should navigate to the add maintenance route with the current motorcycle id", () => {
    const component = new MaintenancePageComponent(maintenanceServiceMock, routeMock, routerMock, swalMock, httpErrorMock);
    component.motorcycleId = "moto-1";

    component.goToAddMaintenance();

    expect(routerMock.navigate).toHaveBeenCalledWith(["/dashboard/motorcycles", "moto-1", "maintenance/add"]);
  });
});
