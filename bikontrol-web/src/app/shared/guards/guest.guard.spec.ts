import { TestBed } from "@angular/core/testing";
import { Router } from "@angular/router";
import { AuthService } from "../../modules/auth/services/auth.service";
import { guestGuard } from "./guest.guard";

describe("guestGuard", () => {
  let authService: any;
  let router: any;

  beforeEach(() => {
    authService = {
      isAuthenticated: vi.fn()
    };
    router = {
      parseUrl: vi.fn((url: string) => ({ redirectedTo: url }))
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: Router, useValue: router }
      ]
    });
  });

  it("should redirect to /dashboard when already authenticated", () => {
    authService.isAuthenticated.mockReturnValue(true);

    const result = TestBed.runInInjectionContext(() => guestGuard({} as any, {} as any));

    expect(router.parseUrl).toHaveBeenCalledWith("/dashboard");
    expect(result).toEqual({ redirectedTo: "/dashboard" });
  });

  it("should allow guests to proceed", () => {
    authService.isAuthenticated.mockReturnValue(false);

    const result = TestBed.runInInjectionContext(() => guestGuard({} as any, {} as any));

    expect(router.parseUrl).not.toHaveBeenCalled();
    expect(result).toBe(true);
  });
});