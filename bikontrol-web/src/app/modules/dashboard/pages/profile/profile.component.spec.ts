import { of, throwError } from "rxjs";
import { ProfileComponent } from "./profile.component";

describe("ProfileComponent (class)", () => {
  const userServiceMock = {
    getMe: jest.fn(),
    updateProfile: jest.fn(),
    changePassword: jest.fn()
  } as any;
  const authServiceMock = {
    isDemo: jest.fn().mockReturnValue(false),
    logout: jest.fn()
  } as any;
  const routerMock = { navigate: jest.fn() } as any;
  const swalMock = {
    error: jest.fn(),
    success: jest.fn().mockReturnValue(Promise.resolve({})),
    confirm: jest.fn()
  } as any;
  const httpErrorMock = {
    message: jest.fn((error: any, fallback = "Error inesperado en el servidor.") => {
      return error?.error?.error || error?.error?.message || error?.message || fallback;
    })
  } as any;

  const profileMock: any = {
    id: "u1",
    email: "santi@bikontrol.com",
    fullName: "Santi Dev",
    role: "User",
    createdAt: "2026-01-01T00:00:00Z",
    hasPassword: true
  };

  function createComponent() {
    return new ProfileComponent(userServiceMock, authServiceMock, routerMock, swalMock, httpErrorMock);
  }

  beforeEach(() => {
    jest.clearAllMocks();
    authServiceMock.isDemo.mockReturnValue(false);
  });

  it("should load the profile on init", () => {
    userServiceMock.getMe.mockReturnValue(of(profileMock));
    const component = createComponent();

    component.ngOnInit();

    expect(userServiceMock.getMe).toHaveBeenCalled();
    expect(component.profile).toEqual(profileMock);
    expect(component.isLoading).toBe(false);
  });

  it("should show an error when the profile fails to load", () => {
    userServiceMock.getMe.mockReturnValue(throwError(() => ({ message: "boom" })));
    const component = createComponent();

    component.ngOnInit();

    expect(component.profile).toBeUndefined();
    expect(swalMock.error).toHaveBeenCalledWith("Error", "boom");
  });

  it("should compute initials from the full name", () => {
    userServiceMock.getMe.mockReturnValue(of(profileMock));
    const component = createComponent();
    component.ngOnInit();

    expect(component.initials).toBe("SD");
  });

  it("should update the name on save", () => {
    userServiceMock.getMe.mockReturnValue(of(profileMock));
    const updated = { ...profileMock, fullName: "Nuevo Nombre" };
    userServiceMock.updateProfile.mockReturnValue(of(updated));
    const component = createComponent();
    component.ngOnInit();

    component.startEditName();
    component.editableName = "Nuevo Nombre";
    component.saveName();

    expect(userServiceMock.updateProfile).toHaveBeenCalledWith("Nuevo Nombre");
    expect(component.profile).toEqual(updated);
    expect(component.isEditingName).toBe(false);
    expect(swalMock.success).toHaveBeenCalled();
  });

  it("should reject an empty name without calling the service", () => {
    userServiceMock.getMe.mockReturnValue(of(profileMock));
    const component = createComponent();
    component.ngOnInit();

    component.startEditName();
    component.editableName = "   ";
    component.saveName();

    expect(userServiceMock.updateProfile).not.toHaveBeenCalled();
    expect(swalMock.error).toHaveBeenCalled();
  });

  it("should change the password when data is valid", () => {
    userServiceMock.getMe.mockReturnValue(of(profileMock));
    userServiceMock.changePassword.mockReturnValue(of({ message: "ok" }));
    const component = createComponent();
    component.ngOnInit();

    component.openPasswordModal();
    component.currentPassword = "old";
    component.newPassword = "newsecret";
    component.confirmPassword = "newsecret";
    component.changePassword();

    expect(userServiceMock.changePassword).toHaveBeenCalledWith("old", "newsecret");
    expect(component.isPasswordModalOpen).toBe(false);
    expect(swalMock.success).toHaveBeenCalled();
  });

  it("should reject mismatched password confirmation without calling the service", () => {
    userServiceMock.getMe.mockReturnValue(of(profileMock));
    const component = createComponent();
    component.ngOnInit();

    component.openPasswordModal();
    component.currentPassword = "old";
    component.newPassword = "newsecret";
    component.confirmPassword = "other";
    component.changePassword();

    expect(userServiceMock.changePassword).not.toHaveBeenCalled();
    expect(swalMock.error).toHaveBeenCalled();
  });

  it("should reject short passwords without calling the service", () => {
    userServiceMock.getMe.mockReturnValue(of(profileMock));
    const component = createComponent();
    component.ngOnInit();

    component.openPasswordModal();
    component.currentPassword = "old";
    component.newPassword = "123";
    component.confirmPassword = "123";
    component.changePassword();

    expect(userServiceMock.changePassword).not.toHaveBeenCalled();
    expect(swalMock.error).toHaveBeenCalled();
  });

  it("should hide password change for google accounts", () => {
    userServiceMock.getMe.mockReturnValue(of({ ...profileMock, hasPassword: false }));
    const component = createComponent();
    component.ngOnInit();

    expect(component.canChangePassword).toBe(false);
  });

  it("should hide password change for demo users", () => {
    authServiceMock.isDemo.mockReturnValue(true);
    userServiceMock.getMe.mockReturnValue(of(profileMock));
    const component = createComponent();
    component.ngOnInit();

    expect(component.isDemo).toBe(true);
    expect(component.canChangePassword).toBe(false);
  });

  it("should logout and navigate to login", () => {
    const component = createComponent();

    component.logout();

    expect(authServiceMock.logout).toHaveBeenCalled();
    expect(routerMock.navigate).toHaveBeenCalledWith(["/login"]);
  });
});
