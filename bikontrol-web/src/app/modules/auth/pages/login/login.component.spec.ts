import { FormBuilder } from "@angular/forms";
import { of, throwError } from "rxjs";
import { LoginComponent } from "./login.component";

describe("LoginComponent", () => {
  let component: LoginComponent;
  let authServiceMock: any;
  let routerMock: any;
  let httpErrorMock: any;

  beforeEach(() => {
    authServiceMock = {
      login: jest.fn()
    };
    routerMock = {
      navigate: jest.fn()
    };
    httpErrorMock = {
      message: jest.fn((error: any, fallback = "Error inesperado en el servidor.") => {
        return error?.error?.error || error?.error?.message || error?.message || fallback;
      })
    };

    component = new LoginComponent(new FormBuilder(), authServiceMock, routerMock, httpErrorMock);
  });

  it("should build a form with required controls", () => {
    expect(component.loginForm.contains("email")).toBe(true);
    expect(component.loginForm.contains("password")).toBe(true);
  });

  it("should mark controls as invalid when touched and empty", () => {
    const email = component.loginForm.get("email");
    email?.markAsTouched();
    expect(component.isInvalid("email")).toBe(true);
  });

  it("should not submit if the form is invalid", () => {
    component.onSubmit();

    expect(component.submitted).toBe(true);
    expect(authServiceMock.login).not.toHaveBeenCalled();
    expect(routerMock.navigate).not.toHaveBeenCalled();
  });

  it("should log in and navigate to dashboard on success", () => {
    authServiceMock.login.mockReturnValue(of({ token: "token-123" }));
    component.loginForm.setValue({
      email: "user@example.com",
      password: "secret1"
    });

    component.onSubmit();

    expect(authServiceMock.login).toHaveBeenCalledWith("user@example.com", "secret1");
    expect(routerMock.navigate).toHaveBeenCalledWith(["/dashboard"]);
    expect(component.errorMessage).toBeNull();
  });

  it("should expose the backend error message on login failure", () => {
    authServiceMock.login.mockReturnValue(
      throwError(() => ({ error: { error: "Credenciales invalidas" } }))
    );
    component.loginForm.setValue({
      email: "user@example.com",
      password: "secret1"
    });

    component.onSubmit();

    expect(httpErrorMock.message).toHaveBeenCalled();
    expect(component.errorMessage).toBe("Credenciales invalidas");
    expect(routerMock.navigate).not.toHaveBeenCalled();
  });

  it("should fallback to a generic error message when backend does not send one", () => {
    authServiceMock.login.mockReturnValue(throwError(() => ({ error: {} })));
    component.loginForm.setValue({
      email: "user@example.com",
      password: "secret1"
    });

    component.onSubmit();

    expect(component.errorMessage).toBe("Error inesperado en el servidor.");
  });
});
