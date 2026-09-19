import { FormBuilder } from "@angular/forms";
import { of, throwError } from "rxjs";
import { ResetPasswordComponent } from "./reset-password.component";

describe("ResetPasswordComponent", () => {
  let component: ResetPasswordComponent;
  let authServiceMock: any;
  let routeMock: any;
  let routerMock: any;
  let httpErrorMock: any;

  beforeEach(() => {
    authServiceMock = {
      resetPassword: jest.fn()
    };
    routeMock = {
      snapshot: {
        queryParamMap: {
          get: jest.fn((key: string) => (key === "token" ? "token-abc" : key === "email" ? "user@example.com" : null))
        }
      }
    };
    routerMock = {
      navigate: jest.fn()
    };
    httpErrorMock = {
      message: jest.fn((error: any, fallback = "Error inesperado en el servidor.") => {
        return error?.error?.error || error?.error?.message || error?.message || fallback;
      })
    };

    component = new ResetPasswordComponent(new FormBuilder(), routeMock, authServiceMock, routerMock, httpErrorMock);
    component.ngOnInit();
  });

  it("should read token and email from the route query params", () => {
    expect(component["token"]).toBe("token-abc");
    expect(component["email"]).toBe("user@example.com");
    expect(component.linkInvalid()).toBe(false);
  });

  it("should mark link invalid when token is missing", () => {
    routeMock.snapshot.queryParamMap.get.mockImplementation((key: string) => (key === "email" ? "user@example.com" : null));
    const c = new ResetPasswordComponent(new FormBuilder(), routeMock, authServiceMock, routerMock, httpErrorMock);
    c.ngOnInit();

    expect(c.linkInvalid()).toBe(true);
  });

  it("should not submit when passwords do not match", () => {
    component.form.setValue({ newPassword: "123456", confirmPassword: "654321" });
    component.onSubmit();

    expect(authServiceMock.resetPassword).not.toHaveBeenCalled();
  });

  it("should call resetPassword and redirect to login on success", () => {
    jest.useFakeTimers();
    authServiceMock.resetPassword.mockReturnValue(of({ message: "Contraseña actualizada." }));
    component.form.setValue({ newPassword: "123456", confirmPassword: "123456" });

    component.onSubmit();

    expect(authServiceMock.resetPassword).toHaveBeenCalledWith("user@example.com", "token-abc", "123456");
    expect(component.successMessage()).toBe("Contraseña actualizada.");
    jest.runAllTimers();
    expect(routerMock.navigate).toHaveBeenCalledWith(["/login"]);
    jest.useRealTimers();
  });

  it("should show the backend error on failure", () => {
    authServiceMock.resetPassword.mockReturnValue(throwError(() => ({ error: { error: "Enlace expirado." } })));
    component.form.setValue({ newPassword: "123456", confirmPassword: "123456" });

    component.onSubmit();

    expect(component.errorMessage()).toBe("Enlace expirado.");
  });
});