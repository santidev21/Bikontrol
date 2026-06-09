import { FormBuilder } from "@angular/forms";
import { convertToParamMap } from "@angular/router";
import { Subject, of, throwError } from "rxjs";
import { SaveMaintenanceComponent } from "./save-maintenance.component";

describe("SaveMaintenanceComponent", () => {
  let component: SaveMaintenanceComponent;
  let maintenanceServiceMock: any;
  let routerMock: any;
  let routeParamMap$: Subject<any>;
  let swalMock: any;

  beforeEach(() => {
    routeParamMap$ = new Subject<any>();
    maintenanceServiceMock = {
      getById: jest.fn(),
      createUserMaintenance: jest.fn(),
      updateMaintenance: jest.fn()
    };
    routerMock = {
      navigate: jest.fn()
    };
    swalMock = {
      error: jest.fn(),
      warning: jest.fn(),
      success: jest.fn().mockResolvedValue(true)
    };

    component = new SaveMaintenanceComponent(
      new FormBuilder(),
      maintenanceServiceMock,
      routerMock,
      {
        snapshot: {
          paramMap: {
            get: jest.fn().mockReturnValue("moto-1")
          }
        },
        paramMap: routeParamMap$.asObservable()
      } as any,
      swalMock
    );
  });

  it("should create the expected form controls", () => {
    expect(component.maintenanceForm.contains("name")).toBe(true);
    expect(component.maintenanceForm.contains("description")).toBe(true);
    expect(component.maintenanceForm.contains("monitoringType")).toBe(true);
    expect(component.maintenanceForm.contains("kmInterval")).toBe(true);
    expect(component.maintenanceForm.contains("timeIntervalWeeks")).toBe(true);
    expect(component.maintenanceForm.contains("timeIntervalUnit")).toBe(true);
  });

  it("should warn when the form is incomplete", () => {
    component.onSubmit();

    expect(swalMock.warning).toHaveBeenCalledWith(
      "Formulario incompleto",
      "Por favor completa todos los campos requeridos."
    );
  });

  it("should create a Km-based maintenance with the motorcycle id from the route", () => {
    component.ngOnInit();
    maintenanceServiceMock.createUserMaintenance.mockReturnValue(of({ id: "maint-1" }));
    component.maintenanceForm.setValue({
      name: "Aceite",
      description: "Cambio de aceite",
      monitoringType: "km",
      kmInterval: 5000,
      timeIntervalWeeks: 1,
      timeIntervalUnit: "weeks"
    });

    component.onSubmit();

    expect(maintenanceServiceMock.createUserMaintenance).toHaveBeenCalledWith({
      motorcycleId: "moto-1",
      name: "Aceite",
      description: "Cambio de aceite",
      monitoringType: "km",
      kmInterval: 5000,
      timeIntervalWeeks: 0,
      timeIntervalUnit: "weeks",
      trackingType: "Km"
    });
  });

  it("should convert months to weeks when creating a time-based maintenance", () => {
    component.ngOnInit();
    maintenanceServiceMock.createUserMaintenance.mockReturnValue(of({ id: "maint-2" }));
    component.maintenanceForm.patchValue({
      name: "Filtro",
      description: "Cambio de filtro",
      monitoringType: "time",
      kmInterval: 1,
      timeIntervalWeeks: 3,
      timeIntervalUnit: "months"
    });

    component.onSubmit();

    expect(maintenanceServiceMock.createUserMaintenance).toHaveBeenCalledWith(
      expect.objectContaining({
        motorcycleId: "moto-1",
        name: "Filtro",
        description: "Cambio de filtro",
        monitoringType: "time",
        kmInterval: 0,
        timeIntervalWeeks: 12,
        timeIntervalUnit: "months",
        trackingType: "Time"
      })
    );
  });

  it("should show an error if there is no motorcycle id when creating", () => {
    component.motorcycleId = "";
    component.maintenanceForm.setValue({
      name: "Aceite",
      description: "Cambio de aceite",
      monitoringType: "km",
      kmInterval: 5000,
      timeIntervalWeeks: 1,
      timeIntervalUnit: "weeks"
    });

    component.addMaintenance({
      name: "Aceite",
      description: "Cambio de aceite",
      monitoringType: "Km",
      kmInterval: 5000,
      timeIntervalWeeks: 0
    } as any);

    expect(swalMock.error).toHaveBeenCalledWith(
      "Error",
      "Debes seleccionar una motocicleta para crear el mantenimiento."
    );
    expect(maintenanceServiceMock.createUserMaintenance).not.toHaveBeenCalled();
  });

  it("should load and map maintenance data in edit mode", () => {
    maintenanceServiceMock.getById.mockReturnValue(
      of({
        id: "maint-1",
        motorcycleId: "moto-2",
        name: "Aceite",
        description: "Cambio de aceite",
        trackingType: "Km",
        isEnabled: true,
        isSystem: false
      })
    );

    component.ngOnInit();
    routeParamMap$.next(convertToParamMap({ id: "maint-1" }));

    expect(component.isEditMode).toBe(true);
    expect(component.maintenanceId).toBe("maint-1");
    expect(component.motorcycleId).toBe("moto-1");
    expect(component.maintenanceForm.get("monitoringType")?.value).toBe("km");
    expect(component.maintenanceForm.get("monitoringType")?.disabled).toBe(true);
  });

  it("should update a maintenance and navigate on success", () => {
    component.ngOnInit();
    component.isEditMode = true;
    component.maintenanceId = "maint-1";
    component.motorcycleId = "moto-1";
    maintenanceServiceMock.updateMaintenance.mockReturnValue(of(undefined));
    component.maintenanceForm.setValue({
      name: "Aceite",
      description: "Cambio de aceite",
      monitoringType: "km",
      kmInterval: 6000,
      timeIntervalWeeks: 1,
      timeIntervalUnit: "weeks"
    });

    component.onSubmit();

    expect(maintenanceServiceMock.updateMaintenance).toHaveBeenCalled();
  });

  it("should surface backend errors when creating maintenance", () => {
    component.ngOnInit();
    maintenanceServiceMock.createUserMaintenance.mockReturnValue(
      throwError(() => ({ error: { message: "No se pudo agregar el mantenimiento." } }))
    );
    component.maintenanceForm.setValue({
      name: "Aceite",
      description: "Cambio de aceite",
      monitoringType: "km",
      kmInterval: 5000,
      timeIntervalWeeks: 1,
      timeIntervalUnit: "weeks"
    });

    component.onSubmit();

    expect(swalMock.error).toHaveBeenCalledWith("Error", "No se pudo agregar el mantenimiento.");
  });

  it("should expose the error helper for touched controls", () => {
    const control = component.maintenanceForm.get("name");
    control?.markAsTouched();

    expect(component.hasError("name", "required")).toBe(true);
  });
});
