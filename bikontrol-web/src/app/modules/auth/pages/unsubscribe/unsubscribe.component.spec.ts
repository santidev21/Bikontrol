import { TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';
import { UnsubscribeComponent } from './unsubscribe.component';
import { AuthService } from '../../services/auth.service';
import { HttpErrorService } from '../../../../shared/services/http-error.service';

describe('UnsubscribeComponent', () => {
  let authServiceMock: { unsubscribeReminderEmails: ReturnType<typeof vi.fn> };

  function createComponent(token: string | null): UnsubscribeComponent {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: { get: () => token } } },
        },
        { provide: AuthService, useValue: authServiceMock },
        {
          provide: HttpErrorService,
          useValue: { message: (error: any) => error?.error?.error ?? 'fallback' },
        },
      ],
    });
    return TestBed.runInInjectionContext(() => new UnsubscribeComponent());
  }

  beforeEach(() => {
    authServiceMock = { unsubscribeReminderEmails: vi.fn() };
  });

  it('does nothing on load: the user has to confirm', () => {
    const component = createComponent('tok.sig');

    expect(component.hasToken).toBe(true);
    expect(component.state()).toBe('ready');
    expect(authServiceMock.unsubscribeReminderEmails).not.toHaveBeenCalled();
  });

  it('posts the token when confirmed and shows success', () => {
    authServiceMock.unsubscribeReminderEmails.mockReturnValue(of(void 0));
    const component = createComponent('tok.sig');

    component.confirm();

    expect(authServiceMock.unsubscribeReminderEmails).toHaveBeenCalledWith('tok.sig');
    expect(component.state()).toBe('done');
  });

  it('shows the error and lets the user retry when the request fails', () => {
    authServiceMock.unsubscribeReminderEmails.mockReturnValue(
      throwError(() => ({ error: { error: 'boom' } })),
    );
    const component = createComponent('tok.sig');

    component.confirm();

    expect(component.state()).toBe('error');
    expect(component.errorMessage()).toBe('boom');
  });

  it('flags a link without a token and never calls the API', () => {
    const component = createComponent(null);

    expect(component.hasToken).toBe(false);
    component.confirm();
    expect(authServiceMock.unsubscribeReminderEmails).not.toHaveBeenCalled();
  });
});
