import { firstValueFrom, of } from "rxjs";
import { UserService } from "./user.service";

describe("UserService (unit, mocked HttpClient)", () => {
  let service: UserService;
  let mockHttp: any;

  beforeEach(() => {
    mockHttp = {
      get: vi.fn(),
      post: vi.fn(),
      put: vi.fn(),
      delete: vi.fn(),
      request: vi.fn()
    };
    service = new UserService(mockHttp as any);
  });

  afterEach(() => vi.resetAllMocks());

  it("should fetch the current user profile", async () => {
    const mock: any = { id: "1", email: "a@b.c", fullName: "Santi", role: "User", createdAt: "2026-01-01", hasPassword: true };
    mockHttp.get.mockReturnValue(of(mock));

    const res = await firstValueFrom(service.getMe());

    expect(res).toEqual(mock);
    expect(mockHttp.get).toHaveBeenCalledWith(`${service["apiUrl"]}/me`);
  });

  it("should update the profile name", async () => {
    const mock: any = { id: "1", email: "a@b.c", fullName: "Nuevo", role: "User", createdAt: "2026-01-01", hasPassword: true };
    mockHttp.put.mockReturnValue(of(mock));

    const res = await firstValueFrom(service.updateProfile("Nuevo"));

    expect(res).toEqual(mock);
    expect(mockHttp.put).toHaveBeenCalledWith(`${service["apiUrl"]}/me`, { fullName: "Nuevo" });
  });

  it("should change the password", async () => {
    const mock: any = { message: "Contraseña actualizada." };
    mockHttp.post.mockReturnValue(of(mock));

    const res = await firstValueFrom(service.changePassword("old", "newsecret"));

    expect(res).toEqual(mock);
    expect(mockHttp.post).toHaveBeenCalledWith(`${service["apiUrl"]}/me/password`, { currentPassword: "old", newPassword: "newsecret" });
  });
});
