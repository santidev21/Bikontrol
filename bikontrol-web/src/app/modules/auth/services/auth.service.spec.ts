import { of } from 'rxjs';
import { AuthService } from './auth.service';

describe('AuthService (unit, mocked HttpClient)', () => {
  let service: AuthService;
  let mockHttp: any;

  beforeEach(() => {
    mockHttp = {
      post: jest.fn()
    };
    localStorage.clear();
    service = new AuthService(mockHttp as any);
  });

  afterEach(() => {
    jest.resetAllMocks();
    localStorage.clear();
  });

  it('login should call POST and store token', done => {
    const mock: any = { token: 'abc123' };
    mockHttp.post.mockReturnValue(of(mock));

    service.login('a@b.com', 'pwd').subscribe(res => {
      expect(res).toEqual(mock);
      expect(localStorage.getItem('token')).toBe('abc123');
      expect(service.isAuthenticated()).toBeTruthy();
      done();
    });
    expect(mockHttp.post).toHaveBeenCalledWith(`${service['apiUrl']}/login`, { email: 'a@b.com', password: 'pwd' });
  });

  it('register should call POST and store token', done => {
    const mock: any = { token: 'reg-token' };
    mockHttp.post.mockReturnValue(of(mock));

    const data = { email: 'x@y.com', password: 'pw' };
    service.register(data).subscribe(res => {
      expect(res).toEqual(mock);
      expect(localStorage.getItem('token')).toBe('reg-token');
      done();
    });
    expect(mockHttp.post).toHaveBeenCalledWith(`${service['apiUrl']}/register`, data);
  });

  it('logout should remove token', () => {
    localStorage.setItem('token', 't');
    service.logout();
    expect(localStorage.getItem('token')).toBeNull();
  });

  it('getToken and isAuthenticated', () => {
    expect(service.getToken()).toBeNull();
    expect(service.isAuthenticated()).toBeFalsy();
    localStorage.setItem('token', 'tok');
    expect(service.getToken()).toBe('tok');
    expect(service.isAuthenticated()).toBeTruthy();
  });
});
