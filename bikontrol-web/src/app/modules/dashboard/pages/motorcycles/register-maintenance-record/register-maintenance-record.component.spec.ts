import { FormBuilder } from "@angular/forms";
import { convertToParamMap } from "@angular/router";
import { Subject, of, throwError } from "rxjs";
import { RegisterMaintenanceRecordComponent } from "./register-maintenance-record.component";

describe("RegisterMaintenanceRecordComponent", () => {
  let component: RegisterMaintenanceRecordComponent;
  let maintenanceServiceMock: any;
  let motorcyclesServiceMock: any;
  let routerMock: any;
  let routeParamMap$: Subject<any>;
  let swalMock: any;

  beforeEach(() => {
    routeParamMap$ = new Subject<any>();
    maintenanceServiceMock = {
      getUserMaintenanceByMotorcycle: jest.fn(),
      getMaintenanceRecordsByMotorcycle: jest.fn(),
      registerMaintenanceRecord: jest.fn()
    };
    motorcyclesServiceMock = {
      getCurrentKm: jest.fn()
    };
    routerMock = {
      navigate: jest.fn()
    };
    swalMock = {
      error: jest.fn(),
      warning: jest.fn(),
      success: jest.fn().mockResolvedValue(true)
    };

    component = new RegisterMaintenanceRecordComponent(
      new FormBuilder(),
      {
        paramMap: routeParamMap$.asObservable()
      } as any,
      routerMock,
      maintenanceServiceMock,
      motorcyclesServiceMock,
      swalMock
    );
  });

  it("should redirect to home when no motorcycle id is present", () => {
    component.ngOnInit();
    routeParamMap$.next(convertToParamMap({}));

    expect(routerMock.navigate).toHaveBeenCalledWith(["/dashboard/home"]);
  });

  it("should load current km and user maintenances for the motorcycle", () => {
    motorcyclesServiceMock.getCurrentKm.mockReturnValue(of({ km: 2300 }));
    maintenanceServiceMock.getUserMaintenanceByMotorcycle.mockReturnValue(of([]));

    component.ngOnInit();
    routeParamMap$.next(convertToParamMap({ motorcycleId: "moto-1" }));

    expect(component.motorcycleId).toBe("moto-1");
    expect(motorcyclesServiceMock.getCurrentKm).toHaveBeenCalledWith("moto-1");
    expect(maintenanceServiceMock.getUserMaintenanceByMotorcycle).toHaveBeenCalledWith("moto-1");
    expect(component.currentKm).toBe(2300);
  });

  it("should update the performed km control when a Km-based maintenance is selected", () => {
    motorcyclesServiceMock.getCurrentKm.mockReturnValue(of({ km: 2300 }));
    maintenanceServiceMock.getUserMaintenanceByMotorcycle.mockReturnValue(
      of([
        {
          id: "maint-1",
          motorcycleId: "moto-1",
          name: "Aceite",
          trackingType: "Km",
          isEnabled: true,
          isSystem: false
        }
      ])
    );
    maintenanceServiceMock.getMaintenanceRecordsByMotorcycle.mockReturnValue(of([]));

    component.ngOnInit();
    routeParamMap$.next(convertToParamMap({ motorcycleId: "moto-1" }));
    component.form.get("userMaintenanceId")?.setValue("maint-1");

    expect(component.selectedMaintenance?.id).toBe("maint-1");
    expect(component.form.get("performedKm")?.value).toBe(2300);
  });

  it("should clear performed km for time-based maintenance", () => {
    motorcyclesServiceMock.getCurrentKm.mockReturnValue(of({ km: 2300 }));
    maintenanceServiceMock.getUserMaintenanceByMotorcycle.mockReturnValue(
      of([
        {
          id: "maint-2",
          motorcycleId: "moto-1",
          name: "Revision",
          trackingType: "Time",
          isEnabled: true,
          isSystem: false
        }
      ])
    );
    maintenanceServiceMock.getMaintenanceRecordsByMotorcycle.mockReturnValue(of([]));

    component.ngOnInit();
    routeParamMap$.next(convertToParamMap({ motorcycleId: "moto-1" }));
    component.form.get("userMaintenanceId")?.setValue("maint-2");

    expect(component.form.get("performedKm")?.value).toBeNull();
  });

  it("should warn when the performed date is in the future", () => {
    component.motorcycleId = "moto-1";
    component.selectedMaintenance = {
      id: "maint-1",
      motorcycleId: "moto-1",
      name: "Aceite",
      trackingType: "Km",
      isEnabled: true,
      isSystem: false
    };
    component.form.patchValue({
      userMaintenanceId: "maint-1",
      performedAt: "2999-01-01",
      performedKm: 2000
    });

    component.onSubmit();

    expect(swalMock.warning).toHaveBeenCalledWith(
      "Error",
      "No puedes agregar mantenimientos posteriores al dia de hoy"
    );
    expect(maintenanceServiceMock.registerMaintenanceRecord).not.toHaveBeenCalled();
  });

  it("should warn when the performed km is lower than the last record", () => {
    motorcyclesServiceMock.getCurrentKm.mockReturnValue(of({ km: 2300 }));
    maintenanceServiceMock.getUserMaintenanceByMotorcycle.mockReturnValue(
      of([
        {
          id: "maint-1",
          motorcycleId: "moto-1",
          name: "Aceite",
          trackingType: "Km",
          isEnabled: true,
          isSystem: false
        }
      ])
    );
    maintenanceServiceMock.getMaintenanceRecordsByMotorcycle.mockReturnValue(
      of([
        {
          userMaintenanceId: "maint-1",
          performedKm: 2200
        }
      ])
    );

    component.ngOnInit();
    routeParamMap$.next(convertToParamMap({ motorcycleId: "moto-1" }));
    component.form.get("userMaintenanceId")?.setValue("maint-1");
    component.form.patchValue({
      performedAt: new Date().toISOString().split("T")[0],
      performedKm: 2100
    });

    component.onSubmit();

    expect(swalMock.warning).toHaveBeenCalledWith(
      "Error",
      "No puedes agregar mantenimiento anterior al ultimo"
    );
  });

  it("should register a maintenance record and navigate to the summary page", async () => {
    motorcyclesServiceMock.getCurrentKm.mockReturnValue(of({ km: 2300 }));
    maintenanceServiceMock.getUserMaintenanceByMotorcycle.mockReturnValue(
      of([
        {
          id: "maint-1",
          motorcycleId: "moto-1",
          name: "Aceite",
          trackingType: "Km",
          isEnabled: true,
          isSystem: false
        }
      ])
    );
    maintenanceServiceMock.getMaintenanceRecordsByMotorcycle.mockReturnValue(of([]));
    maintenanceServiceMock.registerMaintenanceRecord.mockReturnValue(of({ id: "record-1" }));

    component.ngOnInit();
    routeParamMap$.next(convertToParamMap({ motorcycleId: "moto-1" }));
    component.form.get("userMaintenanceId")?.setValue("maint-1");
    component.form.patchValue({
      performedAt: new Date().toISOString().split("T")[0],
      performedKm: 2300
    });

    component.onSubmit();
    await Promise.resolve();
    await Promise.resolve();

    expect(maintenanceServiceMock.registerMaintenanceRecord).toHaveBeenCalledWith({
      motorcycleId: "moto-1",
      userMaintenanceId: "maint-1",
      performedAt: expect.any(String),
      performedKm: 2300
    });
    expect(routerMock.navigate).toHaveBeenCalledWith(["/dashboard/motorcycles/summary"], {
      queryParams: { motorcycleId: "moto-1" }
    });
  });

  it("should surface backend errors when registering a maintenance record", () => {
    motorcyclesServiceMock.getCurrentKm.mockReturnValue(of({ km: 2300 }));
    maintenanceServiceMock.getUserMaintenanceByMotorcycle.mockReturnValue(
      of([
        {
          id: "maint-1",
          motorcycleId: "moto-1",
          name: "Aceite",
          trackingType: "Km",
          isEnabled: true,
          isSystem: false
        }
      ])
    );
    maintenanceServiceMock.getMaintenanceRecordsByMotorcycle.mockReturnValue(of([]));
    maintenanceServiceMock.registerMaintenanceRecord.mockReturnValue(
      throwError(() => ({ error: { error: "No se pudo registrar el mantenimiento." } }))
    );

    component.ngOnInit();
    routeParamMap$.next(convertToParamMap({ motorcycleId: "moto-1" }));
    component.form.get("userMaintenanceId")?.setValue("maint-1");
    component.form.patchValue({
      performedAt: new Date().toISOString().split("T")[0],
      performedKm: 2300
    });

    component.onSubmit();

    expect(swalMock.error).toHaveBeenCalledWith("Error", "No se pudo registrar el mantenimiento.");
  });
});
