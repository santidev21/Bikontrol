import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { FormBuilder } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { LoginComponent } from './login.component';
import { AuthService } from '../../services/auth.service';
import { HttpErrorService } from '../../../../shared/services/http-error.service';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let authServiceMock: any;
  let routerMock: any;
  let httpErrorMock: any;

  beforeEach(() => {
    authServiceMock = {
      login: vi.fn(),
      googleLogin: vi.fn(),
      demoLogin: vi.fn(),
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
    component = TestBed.runInInjectionContext(() => new LoginComponent());
  });

  it('should build a form with required controls', () => {
    expect(component.loginForm.contains('email')).toBe(true);
    expect(component.loginForm.contains('password')).toBe(true);
  });

  it('should mark controls as invalid when touched and empty', () => {
    const email = component.loginForm.get('email');
    email?.markAsTouched();
    expect(component.isInvalid('email')).toBe(true);
  });

  it('should not submit if the form is invalid', () => {
    component.onSubmit();

    expect(component.submitted()).toBe(true);
    expect(authServiceMock.login).not.toHaveBeenCalled();
    expect(routerMock.navigate).not.toHaveBeenCalled();
  });

  it('should log in and navigate to dashboard on success', () => {
    authServiceMock.login.mockReturnValue(of({ token: 'token-123' }));
    component.loginForm.setValue({
      email: 'user@example.com',
      password: 'secret1',
    });

    component.onSubmit();

    expect(authServiceMock.login).toHaveBeenCalledWith('user@example.com', 'secret1');
    expect(routerMock.navigate).toHaveBeenCalledWith(['/dashboard']);
    expect(component.errorMessage()).toBeNull();
  });

  it('should expose the backend error message on login failure', () => {
    authServiceMock.login.mockReturnValue(
      throwError(() => ({ error: { error: 'Credenciales invalidas' } })),
    );
    component.loginForm.setValue({
      email: 'user@example.com',
      password: 'secret1',
    });

    component.onSubmit();

    expect(httpErrorMock.message).toHaveBeenCalled();
    expect(component.errorMessage()).toBe('Credenciales invalidas');
    expect(routerMock.navigate).not.toHaveBeenCalled();
  });

  it('should fallback to a generic error message when backend does not send one', () => {
    authServiceMock.login.mockReturnValue(throwError(() => ({ error: {} })));
    component.loginForm.setValue({
      email: 'user@example.com',
      password: 'secret1',
    });

    component.onSubmit();

    expect(component.errorMessage()).toBe('Error inesperado en el servidor.');
  });

  it('should navigate to dashboard after a successful google login', () => {
    authServiceMock.googleLogin.mockReturnValue(
      of({ token: 'google-token', refreshToken: 'g-refresh' }),
    );

    component.onGoogleCredential({ credential: 'id-token-abc' });

    expect(authServiceMock.googleLogin).toHaveBeenCalledWith('id-token-abc');
    expect(routerMock.navigate).toHaveBeenCalledWith(['/dashboard']);
  });

  it('should offer to resend confirmation when login returns 403', () => {
    authServiceMock.login.mockReturnValue(
      throwError(() => ({ status: 403, error: { error: 'Debes confirmar tu correo' } })),
    );
    component.loginForm.setValue({ email: 'user@example.com', password: 'secret1' });

    component.onSubmit();

    expect(component.canResendConfirmation()).toBe(true);
    expect(component.errorMessage()).toBe('Debes confirmar tu correo');
  });

  it('should resend the confirmation email from the login screen', () => {
    authServiceMock.resendConfirmation.mockReturnValue(of({ message: 'Enviado' }));
    component.loginForm.setValue({ email: 'user@example.com', password: 'secret1' });

    component.onResendConfirmation();

    expect(authServiceMock.resendConfirmation).toHaveBeenCalledWith('user@example.com');
    expect(component.resendMessage()).toBe('Te enviamos un nuevo enlace de confirmación.');
  });

  it('should log in to the demo and navigate when demo is enabled', () => {
    authServiceMock.demoLogin.mockReturnValue(of({ token: 'demo-token' }));

    component.onDemoLogin();

    expect(authServiceMock.demoLogin).toHaveBeenCalled();
    expect(routerMock.navigate).toHaveBeenCalledWith(['/dashboard']);
  });

  it('should not call demoLogin when demo is disabled', () => {
    (component as unknown as { demoEnabled: boolean }).demoEnabled = false;

    component.onDemoLogin();

    expect(authServiceMock.demoLogin).not.toHaveBeenCalled();
    expect(routerMock.navigate).not.toHaveBeenCalled();
  });

  it('should set an error when google credential is missing', () => {
    component.onGoogleCredential({});

    expect(authServiceMock.googleLogin).not.toHaveBeenCalled();
    expect(component.errorMessage()).toContain('Google');
  });

  it('should expose the backend error message on google login failure', () => {
    authServiceMock.googleLogin.mockReturnValue(
      throwError(() => ({ error: { error: 'Token invalido' } })),
    );

    component.onGoogleCredential({ credential: 'id-token-abc' });

    expect(component.errorMessage()).toBe('Token invalido');
    expect(routerMock.navigate).not.toHaveBeenCalled();
  });
});
