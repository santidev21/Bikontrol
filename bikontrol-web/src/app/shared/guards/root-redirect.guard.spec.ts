import { TestBed } from "@angular/core/testing";
import { Router } from "@angular/router";
import { AuthService } from "../../modules/auth/services/auth.service";
import { rootRedirectGuard } from "./root-redirect.guard";

describe("rootRedirectGuard", () => {
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

    const result = TestBed.runInInjectionContext(() => rootRedirectGuard({} as any, {} as any));

    expect(router.parseUrl).toHaveBeenCalledWith("/dashboard");
    expect(result).toEqual({ redirectedTo: "/dashboard" });
  });

  it("should redirect to /login for guests", () => {
    authService.isAuthenticated.mockReturnValue(false);

    const result = TestBed.runInInjectionContext(() => rootRedirectGuard({} as any, {} as any));

    expect(router.parseUrl).toHaveBeenCalledWith("/login");
    expect(result).toEqual({ redirectedTo: "/login" });
  });
});