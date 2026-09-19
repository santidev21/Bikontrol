import { firstValueFrom, of } from "rxjs";
import { AuthService } from "./auth.service";

describe("AuthService (unit, mocked HttpClient)", () => {
  let service: AuthService;
  let mockHttp: any;

  beforeEach(() => {
    mockHttp = {
      post: vi.fn()
    };
    localStorage.clear();
    service = new AuthService(mockHttp as any);
  });

  afterEach(() => {
    vi.resetAllMocks();
    localStorage.clear();
  });

  it("login should call POST and store token + refresh token", async () => {
    const mock: any = { token: "abc123", refreshToken: "refresh-abc" };
    mockHttp.post.mockReturnValue(of(mock));

    const res = await firstValueFrom(service.login("a@b.com", "pwd"));

    expect(res).toEqual(mock);
    expect(localStorage.getItem("token")).toBe("abc123");
    expect(localStorage.getItem("refreshToken")).toBe("refresh-abc");
    expect(service.isAuthenticated()).toBeTruthy();
    expect(mockHttp.post).toHaveBeenCalledWith(`${service["apiUrl"]}/login`, { email: "a@b.com", password: "pwd" });
  });

  it("login should overwrite an existing session", async () => {
    localStorage.setItem("token", "old-token");
    localStorage.setItem("refreshToken", "old-refresh");
    const mock: any = { token: "new-token", refreshToken: "new-refresh" };
    mockHttp.post.mockReturnValue(of(mock));

    await firstValueFrom(service.login("a@b.com", "pwd"));

    expect(localStorage.getItem("token")).toBe("new-token");
    expect(localStorage.getItem("refreshToken")).toBe("new-refresh");
  });

  it("register should call POST and store token + refresh token", async () => {
    const mock: any = { token: "reg-token", refreshToken: "refresh-reg" };
    mockHttp.post.mockReturnValue(of(mock));

    const data = { fullName: "Test User", email: "x@y.com", password: "pw" };
    const res = await firstValueFrom(service.register(data));

    expect(res).toEqual(mock);
    expect(localStorage.getItem("token")).toBe("reg-token");
    expect(localStorage.getItem("refreshToken")).toBe("refresh-reg");
    expect(mockHttp.post).toHaveBeenCalledWith(`${service["apiUrl"]}/register`, data);
  });

  it("googleLogin should call POST /google and store session", async () => {
    const mock: any = { token: "google-token", refreshToken: "google-refresh" };
    mockHttp.post.mockReturnValue(of(mock));

    await firstValueFrom(service.googleLogin("id-token-123"));

    expect(localStorage.getItem("token")).toBe("google-token");
    expect(localStorage.getItem("refreshToken")).toBe("google-refresh");
    expect(mockHttp.post).toHaveBeenCalledWith(`${service["apiUrl"]}/google`, { idToken: "id-token-123" });
  });

  it("forgotPassword should call POST /forgot-password", async () => {
    mockHttp.post.mockReturnValue(of({ message: "ok" }));

    const res = await firstValueFrom(service.forgotPassword("a@b.com"));

    expect(res.message).toBe("ok");
    expect(mockHttp.post).toHaveBeenCalledWith(`${service["apiUrl"]}/forgot-password`, { email: "a@b.com" });
  });

  it("resetPassword should call POST /reset-password", async () => {
    mockHttp.post.mockReturnValue(of({ message: "ok" }));

    const res = await firstValueFrom(service.resetPassword("a@b.com", "tok", "new-pass"));

    expect(res.message).toBe("ok");
    expect(mockHttp.post).toHaveBeenCalledWith(`${service["apiUrl"]}/reset-password`, {
      email: "a@b.com",
      token: "tok",
      newPassword: "new-pass"
    });
  });

  it("refreshSession should call POST /refresh, store new tokens and emit true", async () => {
    localStorage.setItem("refreshToken", "old-refresh");
    const mock: any = { token: "new-token", refreshToken: "new-refresh" };
    mockHttp.post.mockReturnValue(of(mock));

    const ok = await firstValueFrom(service.refreshSession());

    expect(ok).toBe(true);
    expect(localStorage.getItem("token")).toBe("new-token");
    expect(localStorage.getItem("refreshToken")).toBe("new-refresh");
    expect(mockHttp.post).toHaveBeenCalledWith(`${service["apiUrl"]}/refresh`, { refreshToken: "old-refresh" });
  });

  it("refreshSession should error when no refresh token is stored", async () => {
    await expect(firstValueFrom(service.refreshSession())).rejects.toThrow("No refresh token available");
    expect(mockHttp.post).not.toHaveBeenCalled();
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

  it("demoLogin should call POST /demo and store session with Demo role", async () => {
    const mock: any = { token: btoa('h') + '.' + btoa(JSON.stringify({ role: 'Demo' })) + '.' + btoa('s'), refreshToken: "demo-refresh", role: "Demo" };
    mockHttp.post.mockReturnValue(of(mock));

    await firstValueFrom(service.demoLogin());

    expect(localStorage.getItem("token")).toBe(mock.token);
    expect(localStorage.getItem("role")).toBe("Demo");
    expect(service.isDemo()).toBeTruthy();
    expect(service.getRole()).toBe("Demo");
    expect(mockHttp.post).toHaveBeenCalledWith(`${service["apiUrl"]}/demo`, {});
  });

  it("isDemo should decode role from JWT when localStorage empty", () => {
    const payload = btoa(JSON.stringify({ role: "Demo" }));
    const token = `header.${payload}.sig`;
    localStorage.setItem("token", token);
    expect(service.isDemo()).toBeTruthy();
    expect(service.getRole()).toBe("Demo");
  });

  it("logout should clear role", () => {
    localStorage.setItem("role", "Demo");
    service.logout();
    expect(localStorage.getItem("role")).toBeNull();
  });
});
