import { of, throwError } from "rxjs";
import { AuthService } from "./auth.service";

describe("AuthService (unit, mocked HttpClient)", () => {
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

  it("login should call POST and store token + refresh token", done => {
    const mock: any = { token: "abc123", refreshToken: "refresh-abc" };
    mockHttp.post.mockReturnValue(of(mock));

    service.login("a@b.com", "pwd").subscribe(res => {
      expect(res).toEqual(mock);
      expect(localStorage.getItem("token")).toBe("abc123");
      expect(localStorage.getItem("refreshToken")).toBe("refresh-abc");
      expect(service.isAuthenticated()).toBeTruthy();
      done();
    });
    expect(mockHttp.post).toHaveBeenCalledWith(`${service["apiUrl"]}/login`, { email: "a@b.com", password: "pwd" });
  });

  it("login should overwrite an existing session", done => {
    localStorage.setItem("token", "old-token");
    localStorage.setItem("refreshToken", "old-refresh");
    const mock: any = { token: "new-token", refreshToken: "new-refresh" };
    mockHttp.post.mockReturnValue(of(mock));

    service.login("a@b.com", "pwd").subscribe(() => {
      expect(localStorage.getItem("token")).toBe("new-token");
      expect(localStorage.getItem("refreshToken")).toBe("new-refresh");
      done();
    });
  });

  it("register should call POST and store token + refresh token", done => {
    const mock: any = { token: "reg-token", refreshToken: "refresh-reg" };
    mockHttp.post.mockReturnValue(of(mock));

    const data = { email: "x@y.com", password: "pw" };
    service.register(data).subscribe(res => {
      expect(res).toEqual(mock);
      expect(localStorage.getItem("token")).toBe("reg-token");
      expect(localStorage.getItem("refreshToken")).toBe("refresh-reg");
      done();
    });
    expect(mockHttp.post).toHaveBeenCalledWith(`${service["apiUrl"]}/register`, data);
  });

  it("googleLogin should call POST /google and store session", done => {
    const mock: any = { token: "google-token", refreshToken: "google-refresh" };
    mockHttp.post.mockReturnValue(of(mock));

    service.googleLogin("id-token-123").subscribe(res => {
      expect(localStorage.getItem("token")).toBe("google-token");
      expect(localStorage.getItem("refreshToken")).toBe("google-refresh");
      done();
    });
    expect(mockHttp.post).toHaveBeenCalledWith(`${service["apiUrl"]}/google`, { idToken: "id-token-123" });
  });

  it("forgotPassword should call POST /forgot-password", done => {
    mockHttp.post.mockReturnValue(of({ message: "ok" }));

    service.forgotPassword("a@b.com").subscribe(res => {
      expect(res.message).toBe("ok");
      done();
    });
    expect(mockHttp.post).toHaveBeenCalledWith(`${service["apiUrl"]}/forgot-password`, { email: "a@b.com" });
  });

  it("resetPassword should call POST /reset-password", done => {
    mockHttp.post.mockReturnValue(of({ message: "ok" }));

    service.resetPassword("a@b.com", "tok", "new-pass").subscribe(res => {
      expect(res.message).toBe("ok");
      done();
    });
    expect(mockHttp.post).toHaveBeenCalledWith(`${service["apiUrl"]}/reset-password`, {
      email: "a@b.com",
      token: "tok",
      newPassword: "new-pass"
    });
  });

  it("refreshSession should call POST /refresh, store new tokens and emit true", done => {
    localStorage.setItem("refreshToken", "old-refresh");
    const mock: any = { token: "new-token", refreshToken: "new-refresh" };
    mockHttp.post.mockReturnValue(of(mock));

    service.refreshSession().subscribe(ok => {
      expect(ok).toBe(true);
      expect(localStorage.getItem("token")).toBe("new-token");
      expect(localStorage.getItem("refreshToken")).toBe("new-refresh");
      done();
    });
    expect(mockHttp.post).toHaveBeenCalledWith(`${service["apiUrl"]}/refresh`, { refreshToken: "old-refresh" });
  });

  it("refreshSession should error when no refresh token is stored", done => {
    service.refreshSession().subscribe({
      next: () => fail("should not emit"),
      error: err => {
        expect(err.message).toBe("No refresh token available");
        expect(mockHttp.post).not.toHaveBeenCalled();
        done();
      }
    });
  });

  it("logout should remove token and refresh token", () => {
    localStorage.setItem("token", "t");
    localStorage.setItem("refreshToken", "r");
    service.logout();
    expect(localStorage.getItem("token")).toBeNull();
    expect(localStorage.getItem("refreshToken")).toBeNull();
  });

  it("getToken and isAuthenticated", () => {
    expect(service.getToken()).toBeNull();
    expect(service.isAuthenticated()).toBeFalsy();
    localStorage.setItem("token", "tok");
    expect(service.getToken()).toBe("tok");
    expect(service.isAuthenticated()).toBeTruthy();
  });

  it("isAuthenticated should return false when storage is empty", () => {
    expect(service.isAuthenticated()).toBeFalsy();
  });
});