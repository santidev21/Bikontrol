import { TestBed } from '@angular/core/testing';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom, of } from 'rxjs';
import { UserService } from './user.service';

describe('UserService (unit, mocked HttpClient)', () => {
  let service: UserService;
  let mockHttp: any;

  beforeEach(() => {
    mockHttp = {
      get: vi.fn(),
      post: vi.fn(),
      put: vi.fn(),
      delete: vi.fn(),
      request: vi.fn(),
    };
    // The service uses inject(), so it must be created inside an injection context.
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [{ provide: HttpClient, useValue: mockHttp }],
    });
    service = TestBed.runInInjectionContext(() => new UserService());
  });

  afterEach(() => vi.resetAllMocks());

  it('should fetch the current user profile', async () => {
    const mock: any = {
      id: '1',
      email: 'a@b.c',
      fullName: 'Santi',
      role: 'User',
      createdAt: '2026-01-01',
      hasPassword: true,
    };
    mockHttp.get.mockReturnValue(of(mock));

    const res = await firstValueFrom(service.getMe());

    expect(res).toEqual(mock);
    expect(mockHttp.get).toHaveBeenCalledWith(`${service['apiUrl']}/me`);
  });

  it('should update the profile name', async () => {
    const mock: any = {
      id: '1',
      email: 'a@b.c',
      fullName: 'Nuevo',
      role: 'User',
      createdAt: '2026-01-01',
      hasPassword: true,
    };
    mockHttp.put.mockReturnValue(of(mock));

    const res = await firstValueFrom(service.updateProfile('Nuevo'));

    expect(res).toEqual(mock);
    expect(mockHttp.put).toHaveBeenCalledWith(`${service['apiUrl']}/me`, { fullName: 'Nuevo' });
  });

  it('should change the password', async () => {
    const mock: any = { message: 'Contraseña actualizada.' };
    mockHttp.post.mockReturnValue(of(mock));

    const res = await firstValueFrom(service.changePassword('old', 'newsecret'));

    expect(res).toEqual(mock);
    expect(mockHttp.post).toHaveBeenCalledWith(`${service['apiUrl']}/me/password`, {
      currentPassword: 'old',
      newPassword: 'newsecret',
    });
  });

  it('should download the user data export', async () => {
    const mock: any = {
      exportedAt: '2026-01-01T00:00:00Z',
      profile: { id: '1' },
      motorcycles: [],
      maintenances: [],
      maintenanceRecords: [],
      attachments: [],
    };
    mockHttp.get.mockReturnValue(of(mock));

    const res = await firstValueFrom(service.exportMyData());

    expect(res).toEqual(mock);
    expect(mockHttp.get).toHaveBeenCalledWith(`${service['apiUrl']}/me/export`);
  });

  it('should delete the account with the confirmation word', async () => {
    const mock = { message: 'Tu cuenta fue eliminada.' };
    mockHttp.post.mockReturnValue(of(mock));

    const res = await firstValueFrom(service.deleteAccount('secret'));

    expect(res).toEqual(mock);
    expect(mockHttp.post).toHaveBeenCalledWith(`${service['apiUrl']}/me/delete`, {
      confirmation: 'ELIMINAR',
      password: 'secret',
    });
  });
});
