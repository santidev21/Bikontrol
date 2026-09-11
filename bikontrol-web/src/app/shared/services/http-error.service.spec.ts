import { HttpErrorResponse } from "@angular/common/http";
import { HttpErrorService } from "./http-error.service";

describe("HttpErrorService", () => {
  let service: HttpErrorService;

  beforeEach(() => {
    service = new HttpErrorService();
  });

  it("should prefer the backend-provided message when present", () => {
    expect(service.message({ error: { error: "El correo o contraseña son inválidos." } }))
      .toBe("El correo o contraseña son inválidos.");
    expect(service.message({ error: { message: "Otro mensaje." } })).toBe("Otro mensaje.");
  });

  it("should return a friendly message for connection problems (status 0)", () => {
    const error = new HttpErrorResponse({ status: 0, statusText: "Unknown Error" });
    expect(service.message(error)).toContain("No se pudo conectar");
    expect(service.message(error)).not.toContain("Http failure response");
  });

  it("should return a friendly message for a gateway timeout (504)", () => {
    const error = new HttpErrorResponse({ status: 504, statusText: "Gateway Timeout" });
    expect(service.message(error)).toContain("tardando demasiado");
  });

  it("should return a friendly message for server errors", () => {
    expect(service.message(new HttpErrorResponse({ status: 500 }))).toContain("no está disponible");
  });

  it("should return a friendly message for expired sessions (401 without body)", () => {
    expect(service.message(new HttpErrorResponse({ status: 401 }))).toContain("sesión expiró");
  });

  it("should fall back to the generic message when nothing else is available", () => {
    expect(service.message({ error: {} })).toBe("Error inesperado en el servidor.");
    expect(service.message(null)).toBe("Error inesperado en el servidor.");
  });
});