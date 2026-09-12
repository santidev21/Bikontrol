import { HttpRequest, HttpHandlerFn } from "@angular/common/http";
import { authInterceptor } from "./auth.interceptor";
import { of } from "rxjs";

describe("authInterceptor", () => {
  afterEach(() => localStorage.clear());

  it("should add Authorization header when token exists", done => {
    localStorage.setItem("token", "my-token");
    const req = new HttpRequest("GET", "/api/test");
    const next: HttpHandlerFn = (r) => {
      expect(r.headers.get("Authorization")).toBe("Bearer my-token");
      return of(null as any);
    };
    authInterceptor(req, next).subscribe(() => done());
  });

  it("should not add header when no token", done => {
    localStorage.clear();
    const req = new HttpRequest("GET", "/api/test");
    const next: HttpHandlerFn = (r) => {
      expect(r.headers.has("Authorization")).toBeFalsy();
      return of(null as any);
    };
    authInterceptor(req, next).subscribe(() => done());
  });
});
