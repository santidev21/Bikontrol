import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../../auth/services/auth.service';
import { UpdateService } from '../../../../shared/services/update.service';
import { HttpErrorService } from '../../../../shared/services/http-error.service';
import { SwalService } from '../../../../shared/services/swal.service';
import { UserService } from '../../service/user.service';
import { ProfileComponent } from './profile.component';

function fakeResource<T>(initial?: T, initialError?: string) {
  const value = signal<T | undefined>(initial);
  return {
    value,
    error: signal<Error | undefined>(initialError ? new Error(initialError) : undefined),
    hasValue: () => value() !== undefined,
    isLoading: signal(false),
    status: signal('idle'),
    reload: vi.fn(),
    set: (v: T) => value.set(v)
  } as any;
}

const profileMock: any = {
  id: 'u1',
  email: 'santi@bikontrol.com',
  fullName: 'Santi Dev',
  role: 'User',
  createdAt: '2026-01-01T00:00:00Z',
  hasPassword: true
};

describe('ProfileComponent', () => {
  let userServiceMock: any;
  let authServiceMock: any;
  let routerMock: any;
  let swalMock: any;

  beforeEach(() => {
    userServiceMock = {
      getMeResource: vi.fn(() => fakeResource()),
      updateProfile: vi.fn(),
      changePassword: vi.fn()
    };
    authServiceMock = { isDemo: vi.fn().mockReturnValue(false), logout: vi.fn() };
    routerMock = { navigate: vi.fn() };
    swalMock = {
      error: vi.fn(),
      success: vi.fn().mockReturnValue(Promise.resolve({})),
      confirm: vi.fn()
    };

    TestBed.configureTestingModule({
      imports: [ProfileComponent],
      providers: [
        { provide: UserService, useValue: userServiceMock },
        { provide: AuthService, useValue: authServiceMock },
        { provide: Router, useValue: routerMock },
        { provide: SwalService, useValue: swalMock },
        {
          provide: HttpErrorService,
          useValue: { message: (err: any, fallback: string) => err?.error?.error || err?.error?.message || err?.message || fallback }
        },
        { provide: UpdateService, useValue: { appVersion: '0.1.0', swVersion: signal('abc1234') } }
      ]
    });
  });

  function create() {
    return TestBed.createComponent(ProfileComponent);
  }

  it('exposes the profile from the resource', () => {
    userServiceMock.getMeResource.mockReturnValue(fakeResource(profileMock));

    const component = create().componentInstance;

    expect(component.profile()).toEqual(profileMock);
    expect(component.isLoading()).toBe(false);
  });

  it('shows an error when the profile fails to load', () => {
    userServiceMock.getMeResource.mockReturnValue(fakeResource(undefined, 'boom'));

    const fixture = create();
    fixture.detectChanges();

    expect(fixture.componentInstance.profile()).toBeUndefined();
    expect(swalMock.error).toHaveBeenCalledWith('Error', 'boom');
  });

  it('computes initials from the full name', () => {
    userServiceMock.getMeResource.mockReturnValue(fakeResource(profileMock));

    expect(create().componentInstance.initials()).toBe('SD');
  });

  it('updates the name on save', () => {
    userServiceMock.getMeResource.mockReturnValue(fakeResource(profileMock));
    const updated = { ...profileMock, fullName: 'Nuevo Nombre' };
    userServiceMock.updateProfile.mockReturnValue(of(updated));
    const component = create().componentInstance;

    component.startEditName();
    component.editableName.set('Nuevo Nombre');
    component.saveName();

    expect(userServiceMock.updateProfile).toHaveBeenCalledWith('Nuevo Nombre');
    expect(component.profile()).toEqual(updated);
    expect(component.isEditingName()).toBe(false);
    expect(swalMock.success).toHaveBeenCalled();
  });

  it('rejects an empty name without calling the service', () => {
    userServiceMock.getMeResource.mockReturnValue(fakeResource(profileMock));
    const component = create().componentInstance;

    component.startEditName();
    component.editableName.set('   ');
    component.saveName();

    expect(userServiceMock.updateProfile).not.toHaveBeenCalled();
    expect(swalMock.error).toHaveBeenCalled();
  });

  it('changes the password when data is valid', () => {
    userServiceMock.getMeResource.mockReturnValue(fakeResource(profileMock));
    userServiceMock.changePassword.mockReturnValue(of({ message: 'ok' }));
    const component = create().componentInstance;

    component.openPasswordModal();
    component.currentPassword.set('old');
    component.newPassword.set('newsecret');
    component.confirmPassword.set('newsecret');
    component.changePassword();

    expect(userServiceMock.changePassword).toHaveBeenCalledWith('old', 'newsecret');
    expect(component.isPasswordModalOpen()).toBe(false);
    expect(swalMock.success).toHaveBeenCalled();
  });

  it('rejects mismatched password confirmation without calling the service', () => {
    userServiceMock.getMeResource.mockReturnValue(fakeResource(profileMock));
    const component = create().componentInstance;

    component.openPasswordModal();
    component.currentPassword.set('old');
    component.newPassword.set('newsecret');
    component.confirmPassword.set('other');
    component.changePassword();

    expect(userServiceMock.changePassword).not.toHaveBeenCalled();
    expect(swalMock.error).toHaveBeenCalled();
  });

  it('rejects short passwords without calling the service', () => {
    userServiceMock.getMeResource.mockReturnValue(fakeResource(profileMock));
    const component = create().componentInstance;

    component.openPasswordModal();
    component.currentPassword.set('old');
    component.newPassword.set('123');
    component.confirmPassword.set('123');
    component.changePassword();

    expect(userServiceMock.changePassword).not.toHaveBeenCalled();
    expect(swalMock.error).toHaveBeenCalled();
  });

  it('hides password change for google accounts', () => {
    userServiceMock.getMeResource.mockReturnValue(fakeResource({ ...profileMock, hasPassword: false }));

    expect(create().componentInstance.canChangePassword()).toBe(false);
  });

  it('hides password change for demo users', () => {
    authServiceMock.isDemo.mockReturnValue(true);
    userServiceMock.getMeResource.mockReturnValue(fakeResource(profileMock));

    const component = create().componentInstance;

    expect(component.isDemo()).toBe(true);
    expect(component.canChangePassword()).toBe(false);
  });

  it('exposes the app and service worker versions', () => {
    const component = create().componentInstance;

    expect(component.appVersion).toBe('0.1.0');
    expect(component.swVersion()).toBe('abc1234');
  });

  it('logs out and navigates to login', () => {
    const component = create().componentInstance;

    component.logout();

    expect(authServiceMock.logout).toHaveBeenCalled();
    expect(routerMock.navigate).toHaveBeenCalledWith(['/login']);
  });
});
