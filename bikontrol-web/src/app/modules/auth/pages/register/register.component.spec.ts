import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { FormBuilder } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { RegisterComponent } from './register.component';
import { AuthService } from '../../services/auth.service';
import { HttpErrorService } from '../../../../shared/services/http-error.service';

describe('RegisterComponent', () => {
  let component: RegisterComponent;
  let authServiceMock: any;
  let routerMock: any;
  let httpErrorMock: any;

  beforeEach(() => {
    authServiceMock = {
      register: vi.fn(),
      resendConfirmation: vi.fn(),
    };
    routerMock = {
      navigate: vi.fn(),
    };
    httpErrorMock = {
      message: vi.fn((error: any, fallback = 'Error inesperado en el servidor.') => {
        return error?.error?.error || error?.error?.message || error?.message || fallback;
      }),
    };

    // The component uses inject(), so it must be created inside an injection context.
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        { provide: FormBuilder, useValue: new FormBuilder() },
        { provide: AuthService, useValue: authServiceMock },
        { provide: Router, useValue: routerMock },
        { provide: HttpErrorService, useValue: httpErrorMock },
      ],
    });
    component = TestBed.runInInjectionContext(() => new RegisterComponent());
  });

  it('should create the expected form controls', () => {
    expect(component.registerForm.contains('fullName')).toBe(true);
    expect(component.registerForm.contains('email')).toBe(true);
    expect(component.registerForm.contains('password')).toBe(true);
    expect(component.registerForm.contains('confirmPassword')).toBe(true);
  });

  it('should fail password match validation when passwords differ', () => {
    component.registerForm.patchValue({
      fullName: 'Juan Perez',
      email: 'user@example.com',
      password: 'secret1',
      confirmPassword: 'secret2',
    });

    expect(component.registerForm.errors?.['passwordMismatch']).toBe(true);
  });

  it('should not submit invalid forms', () => {
    component.onSubmit();

    expect(component.submitted()).toBe(true);
    expect(authServiceMock.register).not.toHaveBeenCalled();
  });

  it('should register the user and navigate to dashboard', () => {
    authServiceMock.register.mockReturnValue(of({ token: 'token-abc' }));
    component.registerForm.setValue({
      fullName: 'Juan Perez',
      email: 'user@example.com',
      password: 'secret1',
      confirmPassword: 'secret1',
    });

    component.onSubmit();

    expect(authServiceMock.register).toHaveBeenCalledWith({
      fullName: 'Juan Perez',
      email: 'user@example.com',
      password: 'secret1',
    });
    expect(routerMock.navigate).toHaveBeenCalledWith(['/dashboard']);
    expect(component.errorMessage()).toBeNull();
  });

  it('should surface server errors on registration failure', () => {
    authServiceMock.register.mockReturnValue(
      throwError(() => ({ error: { error: 'Email ya registrado' } })),
    );
    component.registerForm.setValue({
      fullName: 'Juan Perez',
      email: 'user@example.com',
      password: 'secret1',
      confirmPassword: 'secret1',
    });

    component.onSubmit();

    expect(httpErrorMock.message).toHaveBeenCalled();
    expect(component.errorMessage()).toBe('Email ya registrado');
    expect(routerMock.navigate).not.toHaveBeenCalled();
  });

  it('should show the confirmation screen instead of navigating when confirmation is required', () => {
    authServiceMock.register.mockReturnValue(
      of({ email: 'user@example.com', emailConfirmationRequired: true }),
    );
    component.registerForm.setValue({
      fullName: 'Juan Perez',
      email: 'user@example.com',
      password: 'secret1',
      confirmPassword: 'secret1',
    });

    component.onSubmit();

    expect(component.confirmationSent()).toBe(true);
    expect(routerMock.navigate).not.toHaveBeenCalled();
  });

  it('should resend the confirmation email', () => {
    authServiceMock.register.mockReturnValue(
      of({ email: 'user@example.com', emailConfirmationRequired: true }),
    );
    authServiceMock.resendConfirmation.mockReturnValue(of({ message: 'Enviado' }));
    component.registerForm.setValue({
      fullName: 'Juan Perez',
      email: 'user@example.com',
      password: 'secret1',
      confirmPassword: 'secret1',
    });
    component.onSubmit();

    component.onResendConfirmation();

    expect(authServiceMock.resendConfirmation).toHaveBeenCalledWith('user@example.com');
    expect(component.resendMessage()).toBe('Enviado');
  });

  it('should use the generic fallback message when the backend response is empty', () => {
    authServiceMock.register.mockReturnValue(throwError(() => ({ error: {} })));
    component.registerForm.setValue({
      fullName: 'Juan Perez',
      email: 'user@example.com',
      password: 'secret1',
      confirmPassword: 'secret1',
    });

    component.onSubmit();

    expect(component.errorMessage()).toBe('Error inesperado en el servidor.');
  });
});
