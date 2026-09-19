import { HttpRequest, HttpHandlerFn } from "@angular/common/http";
import { authInterceptor } from "./auth.interceptor";
import { firstValueFrom, of } from "rxjs";

describe("authInterceptor", () => {
  afterEach(() => localStorage.clear());

  it("should add Authorization header when token exists", async () => {
    localStorage.setItem("token", "my-token");
    const req = new HttpRequest("GET", "/api/test");
    const next: HttpHandlerFn = (r) => {
      expect(r.headers.get("Authorization")).toBe("Bearer my-token");
      return of(null as any);
    };

    await firstValueFrom(authInterceptor(req, next));
  });

  it("should not add header when no token", async () => {
    localStorage.clear();
    const req = new HttpRequest("GET", "/api/test");
    const next: HttpHandlerFn = (r) => {
      expect(r.headers.has("Authorization")).toBeFalsy();
      return of(null as any);
    };

    await firstValueFrom(authInterceptor(req, next));
  });
});
