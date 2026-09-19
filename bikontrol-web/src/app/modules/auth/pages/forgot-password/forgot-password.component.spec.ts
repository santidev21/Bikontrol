import { FormBuilder } from "@angular/forms";
import { of, throwError } from "rxjs";
import { ForgotPasswordComponent } from "./forgot-password.component";

describe("ForgotPasswordComponent", () => {
  let component: ForgotPasswordComponent;
  let authServiceMock: any;
  let httpErrorMock: any;

  beforeEach(() => {
    authServiceMock = {
      forgotPassword: jest.fn()
    };
    httpErrorMock = {
      message: jest.fn((error: any, fallback = "Error inesperado en el servidor.") => {
        return error?.error?.error || error?.error?.message || error?.message || fallback;
      })
    };

    component = new ForgotPasswordComponent(new FormBuilder(), authServiceMock, httpErrorMock);
  });

  it("should build a form with an email control", () => {
    expect(component.form.contains("email")).toBe(true);
  });

  it("should not submit if the form is invalid", () => {
    component.onSubmit();

    expect(authServiceMock.forgotPassword).not.toHaveBeenCalled();
  });

  it("should call forgotPassword and show the success message", () => {
    authServiceMock.forgotPassword.mockReturnValue(of({ message: "Revisa tu correo." }));
    component.form.setValue({ email: "user@example.com" });

    component.onSubmit();

    expect(authServiceMock.forgotPassword).toHaveBeenCalledWith("user@example.com");
    expect(component.successMessage()).toBe("Revisa tu correo.");
    expect(component.errorMessage()).toBeNull();
  });

  it("should show the backend error on failure", () => {
    authServiceMock.forgotPassword.mockReturnValue(throwError(() => ({ error: { error: "Error" } })));
    component.form.setValue({ email: "user@example.com" });

    component.onSubmit();

    expect(component.errorMessage()).toBe("Error");
  });
});