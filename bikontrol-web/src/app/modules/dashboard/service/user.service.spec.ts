import { of } from "rxjs";
import { UserService } from "./user.service";

describe("UserService (unit, mocked HttpClient)", () => {
  let service: UserService;
  let mockHttp: any;

  beforeEach(() => {
    mockHttp = {
      get: jest.fn(),
      post: jest.fn(),
      put: jest.fn(),
      delete: jest.fn(),
      request: jest.fn()
    };
    service = new UserService(mockHttp as any);
  });

  afterEach(() => jest.resetAllMocks());

  it("should fetch the current user profile", done => {
    const mock: any = { id: "1", email: "a@b.c", fullName: "Santi", role: "User", createdAt: "2026-01-01", hasPassword: true };
    mockHttp.get.mockReturnValue(of(mock));

    service.getMe().subscribe(res => {
      expect(res).toEqual(mock);
      done();
    });
    expect(mockHttp.get).toHaveBeenCalledWith(`${service["apiUrl"]}/me`);
  });

  it("should update the profile name", done => {
    const mock: any = { id: "1", email: "a@b.c", fullName: "Nuevo", role: "User", createdAt: "2026-01-01", hasPassword: true };
    mockHttp.put.mockReturnValue(of(mock));

    service.updateProfile("Nuevo").subscribe(res => {
      expect(res).toEqual(mock);
      done();
    });
    expect(mockHttp.put).toHaveBeenCalledWith(`${service["apiUrl"]}/me`, { fullName: "Nuevo" });
  });

  it("should change the password", done => {
    const mock: any = { message: "Contraseña actualizada." };
    mockHttp.post.mockReturnValue(of(mock));

    service.changePassword("old", "newsecret").subscribe(res => {
      expect(res).toEqual(mock);
      done();
    });
    expect(mockHttp.post).toHaveBeenCalledWith(`${service["apiUrl"]}/me/password`, { currentPassword: "old", newPassword: "newsecret" });
  });
});
