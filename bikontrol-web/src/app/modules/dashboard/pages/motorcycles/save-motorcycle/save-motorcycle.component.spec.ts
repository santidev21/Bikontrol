import { FormBuilder } from "@angular/forms";
import { convertToParamMap } from "@angular/router";
import { Subject, of, throwError } from "rxjs";
import { SaveMotorcycleComponent } from "./save-motorcycle.component";
import { resizeImageFile } from "../../../../../shared/utils/image.utils";

jest.mock("../../../../../shared/utils/image.utils", () => ({
  resizeImageFile: jest.fn()
}));

describe("SaveMotorcycleComponent", () => {
  let component: SaveMotorcycleComponent;
  let motorcyclesServiceMock: any;
  let routerMock: any;
  let routeParamMap$: Subject<any>;
  let swalMock: any;
  let httpErrorMock: any;

  beforeEach(() => {
    routeParamMap$ = new Subject<any>();
    motorcyclesServiceMock = {
      getById: jest.fn(),
      getCurrentKm: jest.fn(),
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
    httpErrorMock = {
      message: jest.fn(
        (error: any, fallback = "Error inesperado en el servidor.") =>
          error?.error?.error || error?.error?.message || error?.message || fallback
      )
    };

    component = new SaveMotorcycleComponent(
      new FormBuilder(),
      motorcyclesServiceMock,
      routerMock,
      {
        paramMap: routeParamMap$.asObservable()
      } as any,
      swalMock,
      httpErrorMock
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
        km: 0,
        displacement: 150,
        plate: "XYZ789",
        image: "default.png",
        isEnabled: true
      })
    );
    motorcyclesServiceMock.getCurrentKm.mockReturnValue(of({ km: 2000 }));

    component.ngOnInit();
    routeParamMap$.next(convertToParamMap({ id: "moto-2" }));

    expect(component.isEditMode()).toBe(true);
    expect(component.motorcycleId()).toBe("moto-2");
    expect(motorcyclesServiceMock.getById).toHaveBeenCalledWith("moto-2");
    expect(component.motorcycleForm.get("name")?.value).toBe("XTZ");
  });

  it("should show the current km and disable it in edit mode", () => {
    motorcyclesServiceMock.getById.mockReturnValue(
      of({
        id: "moto-2",
        name: "XTZ",
        brand: "Yamaha",
        year: 2023,
        nickname: "La negra",
        km: 0,
        displacement: 150,
        plate: "XYZ789",
        image: "default.png",
        isEnabled: true
      })
    );
    motorcyclesServiceMock.getCurrentKm.mockReturnValue(of({ km: 4500 }));

    component.ngOnInit();
    routeParamMap$.next(convertToParamMap({ id: "moto-2" }));

    expect(motorcyclesServiceMock.getCurrentKm).toHaveBeenCalledWith("moto-2");
    expect(component.motorcycleForm.get("km")?.value).toBe(4500);
    expect(component.motorcycleForm.get("km")?.disabled).toBe(true);
  });

  it("should update a motorcycle and navigate on success", async () => {
    component.isEditMode.set(true);
    component.motorcycleId.set("moto-2");
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

  it("should show the placeholder preview by default", () => {
    component.motorcycleForm.patchValue({ image: "default.png" });

    expect(component.previewSrc()).toBe("/assets/images/defaults/motorcycle-placeholder.webp");
  });

  it("should set the preview from the loaded motorcycle image", () => {
    motorcyclesServiceMock.getById.mockReturnValue(
      of({
        id: "moto-2",
        name: "XTZ",
        brand: "Yamaha",
        year: 2023,
        nickname: "La negra",
        km: 0,
        displacement: 150,
        plate: "XYZ789",
        image: "data:image/jpeg;base64,abc",
        isEnabled: true
      })
    );
    motorcyclesServiceMock.getCurrentKm.mockReturnValue(of({ km: 2000 }));

    component.ngOnInit();
    routeParamMap$.next(convertToParamMap({ id: "moto-2" }));

    expect(component.previewSrc()).toBe("data:image/jpeg;base64,abc");
  });

  it("should reset the image to the default when removing it", () => {
    component.previewSrc.set("data:image/jpeg;base64,abc");

    component.removeImage();

    expect(component.motorcycleForm.get("image")?.value).toBe("default.png");
    expect(component.previewSrc()).toBe("/assets/images/defaults/motorcycle-placeholder.webp");
  });

  it("should reject non-image files", () => {
    const input = { value: "x", files: [{ name: "doc.pdf", type: "application/pdf", size: 100 }] } as any;

    component.onImageSelected({ target: input } as any);

    expect(swalMock.warning).toHaveBeenCalledWith("Archivo inválido", "Selecciona un archivo de imagen válido.");
    expect(input.value).toBe("");
  });

  it("should reject images larger than 2 MB", () => {
    const input = { value: "x", files: [{ name: "big.png", type: "image/png", size: 3 * 1024 * 1024 }] } as any;

    component.onImageSelected({ target: input } as any);

    expect(swalMock.warning).toHaveBeenCalledWith("Archivo muy grande", "La imagen no puede superar 2 MB.");
    expect(input.value).toBe("");
  });

  it("should resize a valid image and store it in the form", async () => {
    const resizeMock = resizeImageFile as jest.Mock;
    resizeMock.mockResolvedValue("data:image/jpeg;base64,resized");
    const input = { value: "x", files: [{ name: "moto.png", type: "image/png", size: 500 }] } as any;

    component.onImageSelected({ target: input } as any);
    await Promise.resolve();

    expect(resizeMock).toHaveBeenCalledWith(input.files[0]);
    expect(component.motorcycleForm.get("image")?.value).toBe("data:image/jpeg;base64,resized");
    expect(component.previewSrc()).toBe("data:image/jpeg;base64,resized");
  });

  it("should warn and reset the input when the image cannot be read", async () => {
    const resizeMock = resizeImageFile as jest.Mock;
    resizeMock.mockRejectedValue(new Error("decode-failed"));
    const input = { value: "x", files: [{ name: "moto.png", type: "image/png", size: 500 }] } as any;

    component.onImageSelected({ target: input } as any);
    await Promise.resolve();
    await Promise.resolve();

    expect(swalMock.warning).toHaveBeenCalledWith("Archivo inválido", "No se pudo leer la imagen seleccionada.");
    expect(input.value).toBe("");
  });
});
