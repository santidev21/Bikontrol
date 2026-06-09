import { FormBuilder } from "@angular/forms";
import { of, throwError } from "rxjs";
import { RegisterComponent } from "./register.component";

describe("RegisterComponent", () => {
  let component: RegisterComponent;
  let authServiceMock: any;
  let routerMock: any;
  let httpErrorMock: any;

  beforeEach(() => {
    authServiceMock = {
      register: jest.fn()
    };
    routerMock = {
      navigate: jest.fn()
    };
    httpErrorMock = {
      message: jest.fn((error: any, fallback = "Error inesperado en el servidor.") => {
        return error?.error?.error || error?.error?.message || error?.message || fallback;
      })
    };

    component = new RegisterComponent(new FormBuilder(), authServiceMock, routerMock, httpErrorMock);
  });

  it("should create the expected form controls", () => {
    expect(component.registerForm.contains("fullName")).toBe(true);
    expect(component.registerForm.contains("email")).toBe(true);
    expect(component.registerForm.contains("password")).toBe(true);
    expect(component.registerForm.contains("confirmPassword")).toBe(true);
  });

  it("should fail password match validation when passwords differ", () => {
    component.registerForm.patchValue({
      fullName: "Juan Perez",
      email: "user@example.com",
      password: "secret1",
      confirmPassword: "secret2"
    });

    expect(component.registerForm.errors?.["passwordMismatch"]).toBe(true);
  });

  it("should not submit invalid forms", () => {
    component.onSubmit();

    expect(component.submitted).toBe(true);
    expect(authServiceMock.register).not.toHaveBeenCalled();
  });

  it("should register the user and navigate to dashboard", () => {
    authServiceMock.register.mockReturnValue(of({ token: "token-abc" }));
    component.registerForm.setValue({
      fullName: "Juan Perez",
      email: "user@example.com",
      password: "secret1",
      confirmPassword: "secret1"
    });

    component.onSubmit();

    expect(authServiceMock.register).toHaveBeenCalledWith({
      fullName: "Juan Perez",
      email: "user@example.com",
      password: "secret1"
    });
    expect(routerMock.navigate).toHaveBeenCalledWith(["/dashboard"]);
    expect(component.errorMessage).toBeNull();
  });

  it("should surface server errors on registration failure", () => {
    authServiceMock.register.mockReturnValue(
      throwError(() => ({ error: { error: "Email ya registrado" } }))
    );
    component.registerForm.setValue({
      fullName: "Juan Perez",
      email: "user@example.com",
      password: "secret1",
      confirmPassword: "secret1"
    });

    component.onSubmit();

    expect(httpErrorMock.message).toHaveBeenCalled();
    expect(component.errorMessage).toBe("Email ya registrado");
    expect(routerMock.navigate).not.toHaveBeenCalled();
  });

  it("should use the generic fallback message when the backend response is empty", () => {
    authServiceMock.register.mockReturnValue(throwError(() => ({ error: {} })));
    component.registerForm.setValue({
      fullName: "Juan Perez",
      email: "user@example.com",
      password: "secret1",
      confirmPassword: "secret1"
    });

    component.onSubmit();

    expect(component.errorMessage).toBe("Error inesperado en el servidor.");
  });
});
