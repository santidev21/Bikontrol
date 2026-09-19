import { TestBed } from "@angular/core/testing";
import { Router } from "@angular/router";
import { AuthService } from "../../modules/auth/services/auth.service";
import { authGuard } from "./auth.guard";

describe("authGuard", () => {
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

  it("should allow authenticated users to proceed", () => {
    authService.isAuthenticated.mockReturnValue(true);

    const result = TestBed.runInInjectionContext(() => authGuard({} as any, {} as any));

    expect(router.parseUrl).not.toHaveBeenCalled();
    expect(result).toBe(true);
  });

  it("should redirect to /login when not authenticated", () => {
    authService.isAuthenticated.mockReturnValue(false);

    const result = TestBed.runInInjectionContext(() => authGuard({} as any, {} as any));

    expect(router.parseUrl).toHaveBeenCalledWith("/login");
    expect(result).toEqual({ redirectedTo: "/login" });
  });
});
