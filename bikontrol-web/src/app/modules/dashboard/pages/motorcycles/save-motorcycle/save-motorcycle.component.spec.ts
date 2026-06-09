import { FormBuilder } from "@angular/forms";
import { convertToParamMap } from "@angular/router";
import { Subject, of, throwError } from "rxjs";
import { SaveMotorcycleComponent } from "./save-motorcycle.component";

describe("SaveMotorcycleComponent", () => {
  let component: SaveMotorcycleComponent;
  let motorcyclesServiceMock: any;
  let routerMock: any;
  let routeParamMap$: Subject<any>;
  let swalMock: any;

  beforeEach(() => {
    routeParamMap$ = new Subject<any>();
    motorcyclesServiceMock = {
      getById: jest.fn(),
      addMotorcycle: jest.fn(),
      updateMotorcycle: jest.fn()
    };
    routerMock = {
      navigate: jest.fn()
    };
    swalMock = {
      error: jest.fn(),
      warning: jest.fn(),
      success: jest.fn().mockResolvedValue(true)
    };

    component = new SaveMotorcycleComponent(
      new FormBuilder(),
      motorcyclesServiceMock,
      routerMock,
      {
        paramMap: routeParamMap$.asObservable()
      } as any,
      swalMock
    );
  });

  it("should create the expected form controls", () => {
    expect(component.motorcycleForm.contains("name")).toBe(true);
    expect(component.motorcycleForm.contains("brand")).toBe(true);
    expect(component.motorcycleForm.contains("year")).toBe(true);
    expect(component.motorcycleForm.contains("nickname")).toBe(true);
    expect(component.motorcycleForm.contains("km")).toBe(true);
    expect(component.motorcycleForm.contains("displacement")).toBe(true);
    expect(component.motorcycleForm.contains("plate")).toBe(true);
  });

  it("should show a warning when submitting an invalid form", () => {
    component.onSubmit();

    expect(swalMock.warning).toHaveBeenCalledWith(
      "Formulario incompleto",
      "Por favor completa todos los campos requeridos."
    );
    expect(motorcyclesServiceMock.addMotorcycle).not.toHaveBeenCalled();
    expect(motorcyclesServiceMock.updateMotorcycle).not.toHaveBeenCalled();
  });

  it("should add a motorcycle and navigate on success", async () => {
    motorcyclesServiceMock.addMotorcycle.mockReturnValue(of({ id: "moto-1" }));
    component.motorcycleForm.setValue({
      name: "XTZ",
      brand: "Yamaha",
      year: 2024,
      nickname: "La azul",
      km: 1000,
      displacement: 150,
      plate: "ABC123",
      image: "default.png",
      isEnabled: true
    });

    component.onSubmit();
    await Promise.resolve();
    await Promise.resolve();

    expect(motorcyclesServiceMock.addMotorcycle).toHaveBeenCalledWith({
      name: "XTZ",
      brand: "Yamaha",
      year: 2024,
      nickname: "La azul",
      km: 1000,
      displacement: 150,
      plate: "ABC123",
      image: "default.png",
      isEnabled: true
    });
    expect(swalMock.success).toHaveBeenCalledWith("¡Éxito!", "Motocicleta agregada correctamente.");
    expect(routerMock.navigate).toHaveBeenCalledWith(["/dashboard"]);
  });

  it("should surface backend errors when adding a motorcycle", () => {
    motorcyclesServiceMock.addMotorcycle.mockReturnValue(
      throwError(() => ({ error: { message: "No se pudo agregar la motocicleta." } }))
    );
    component.motorcycleForm.setValue({
      name: "XTZ",
      brand: "Yamaha",
      year: 2024,
      nickname: "La azul",
      km: 1000,
      displacement: 150,
      plate: "ABC123",
      image: "default.png",
      isEnabled: true
    });

    component.onSubmit();

    expect(swalMock.error).toHaveBeenCalledWith("Error", "No se pudo agregar la motocicleta.");
    expect(routerMock.navigate).not.toHaveBeenCalled();
  });

  it("should load a motorcycle when the route contains an id", () => {
    motorcyclesServiceMock.getById.mockReturnValue(
      of({
        id: "moto-2",
        name: "XTZ",
        brand: "Yamaha",
        year: 2023,
        nickname: "La negra",
        km: 2000,
        displacement: 150,
        plate: "XYZ789",
        image: "default.png",
        isEnabled: true
      })
    );

    component.ngOnInit();
    routeParamMap$.next(convertToParamMap({ id: "moto-2" }));

    expect(component.isEditMode).toBe(true);
    expect(component.motorcycleId).toBe("moto-2");
    expect(motorcyclesServiceMock.getById).toHaveBeenCalledWith("moto-2");
    expect(component.motorcycleForm.get("name")?.value).toBe("XTZ");
  });

  it("should update a motorcycle and navigate on success", async () => {
    component.isEditMode = true;
    component.motorcycleId = "moto-2";
    motorcyclesServiceMock.updateMotorcycle.mockReturnValue(of(undefined));
    component.motorcycleForm.setValue({
      name: "XTZ",
      brand: "Yamaha",
      year: 2024,
      nickname: "La azul",
      km: 1200,
      displacement: 150,
      plate: "ABC123",
      image: "default.png",
      isEnabled: true
    });

    component.onSubmit();
    await Promise.resolve();
    await Promise.resolve();

    expect(motorcyclesServiceMock.updateMotorcycle).toHaveBeenCalledWith("moto-2", {
      name: "XTZ",
      brand: "Yamaha",
      year: 2024,
      nickname: "La azul",
      km: 1200,
      displacement: 150,
      plate: "ABC123",
      image: "default.png",
      isEnabled: true
    });
    expect(swalMock.success).toHaveBeenCalledWith("¡Éxito!", "Motocicleta actualizada correctamente.");
    expect(routerMock.navigate).toHaveBeenCalledWith(["/dashboard"]);
  });

  it("should expose the error helper when a field is touched and invalid", () => {
    const control = component.motorcycleForm.get("name");
    control?.markAsTouched();

    expect(component.hasError("name", "required")).toBe(true);
  });
});
