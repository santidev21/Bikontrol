import { TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ConfirmEmailComponent } from './confirm-email.component';
import { AuthService } from '../../services/auth.service';
import { HttpErrorService } from '../../../../shared/services/http-error.service';

describe('ConfirmEmailComponent', () => {
  let authServiceMock: any;
  let routeMock: any;
  let httpErrorMock: any;

  // The component uses inject(), so it must be created inside an injection context.
  function createComponent(): ConfirmEmailComponent {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        { provide: ActivatedRoute, useValue: routeMock },
        { provide: AuthService, useValue: authServiceMock },
        { provide: HttpErrorService, useValue: httpErrorMock },
      ],
    });
    return TestBed.runInInjectionContext(() => new ConfirmEmailComponent());
  }

  beforeEach(() => {
    authServiceMock = {
      confirmEmail: vi.fn(),
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
    httpErrorMock = {
      message: vi.fn((error: any, fallback = 'Error inesperado en el servidor.') => {
        return error?.error?.error || error?.error?.message || error?.message || fallback;
      }),
    };
  });

  it('should confirm the email on init and show success', () => {
    authServiceMock.confirmEmail.mockReturnValue(of({ message: 'Correo confirmado.' }));

    const component = createComponent();
    component.ngOnInit();

    expect(authServiceMock.confirmEmail).toHaveBeenCalledWith('user@example.com', 'token-abc');
    expect(component.success()).toBe(true);
    expect(component.successMessage()).toBe('Correo confirmado. Ya puedes iniciar sesión.');
    expect(component.loading()).toBe(false);
  });

  it('should surface the backend error when the link is invalid', () => {
    authServiceMock.confirmEmail.mockReturnValue(
      throwError(() => ({ error: { error: 'El enlace de confirmación no es válido.' } })),
    );

    const component = createComponent();
    component.ngOnInit();

    expect(component.success()).toBe(false);
    expect(component.errorMessage()).toBe('El enlace de confirmación no es válido.');
    expect(component.loading()).toBe(false);
  });

  it('should not call the API when the link is incomplete', () => {
    routeMock.snapshot.queryParamMap.get.mockReturnValue(null);

    const component = createComponent();
    component.ngOnInit();

    expect(authServiceMock.confirmEmail).not.toHaveBeenCalled();
    expect(component.errorMessage()).toContain('inválido');
  });
});
