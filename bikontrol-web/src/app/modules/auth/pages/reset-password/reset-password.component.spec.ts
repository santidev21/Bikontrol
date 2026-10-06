import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { ResetPasswordComponent } from './reset-password.component';
import { AuthService } from '../../services/auth.service';
import { HttpErrorService } from '../../../../shared/services/http-error.service';

describe('ResetPasswordComponent', () => {
  let component: ResetPasswordComponent;
  let authServiceMock: any;
  let routeMock: any;
  let routerMock: any;
  let httpErrorMock: any;

  // The component uses inject(), so it must be created inside an injection context.
  function createComponent(): ResetPasswordComponent {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        { provide: FormBuilder, useValue: new FormBuilder() },
        { provide: ActivatedRoute, useValue: routeMock },
        { provide: AuthService, useValue: authServiceMock },
        { provide: Router, useValue: routerMock },
        { provide: HttpErrorService, useValue: httpErrorMock },
      ],
    });
    return TestBed.runInInjectionContext(() => new ResetPasswordComponent());
  }

  beforeEach(() => {
    authServiceMock = {
      resetPassword: vi.fn(),
    };
    routeMock = {
      snapshot: {
        queryParamMap: {
          get: vi.fn((key: string) =>
            key === 'token' ? 'token-abc' : key === 'email' ? 'user@example.com' : null,
          ),
        },
      },
    };
    routerMock = {
      navigate: vi.fn(),
    };
    httpErrorMock = {
      message: vi.fn((error: any, fallback = 'Error inesperado en el servidor.') => {
        return error?.error?.error || error?.error?.message || error?.message || fallback;
      }),
    };

    component = createComponent();
    component.ngOnInit();
  });

  it('should read token and email from the route query params', () => {
    expect(component['token']).toBe('token-abc');
    expect(component['email']).toBe('user@example.com');
    expect(component.linkInvalid()).toBe(false);
  });

  it('should mark link invalid when token is missing', () => {
    routeMock.snapshot.queryParamMap.get.mockImplementation((key: string) =>
      key === 'email' ? 'user@example.com' : null,
    );
    const c = createComponent();
    c.ngOnInit();

    expect(c.linkInvalid()).toBe(true);
  });

  it('should not submit when passwords do not match', () => {
    component.form.setValue({ newPassword: '123456', confirmPassword: '654321' });
    component.onSubmit();

    expect(authServiceMock.resetPassword).not.toHaveBeenCalled();
  });

  it('should call resetPassword and redirect to login on success', () => {
    vi.useFakeTimers();
    authServiceMock.resetPassword.mockReturnValue(of({ message: 'Contraseña actualizada.' }));
    component.form.setValue({ newPassword: '123456', confirmPassword: '123456' });

    component.onSubmit();

    expect(authServiceMock.resetPassword).toHaveBeenCalledWith(
      'user@example.com',
      'token-abc',
      '123456',
    );
    expect(component.successMessage()).toBe('Contraseña actualizada. Ya puedes iniciar sesión.');
    vi.runAllTimers();
    expect(routerMock.navigate).toHaveBeenCalledWith(['/login']);
    vi.useRealTimers();
  });

  it('should show the backend error on failure', () => {
    authServiceMock.resetPassword.mockReturnValue(
      throwError(() => ({ error: { error: 'Enlace expirado.' } })),
    );
    component.form.setValue({ newPassword: '123456', confirmPassword: '123456' });

    component.onSubmit();

    expect(component.errorMessage()).toBe('Enlace expirado.');
  });
});
