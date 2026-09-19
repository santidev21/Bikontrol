import { Subject } from "rxjs";
import { UpdateService } from "./update.service";

describe("UpdateService (class)", () => {
  let versionUpdates$: Subject<any>;
  let swUpdateMock: any;
  let swalMock: any;
  const originalFetch = (globalThis as any).fetch;

  function createService() {
    return new UpdateService(swUpdateMock, swalMock);
  }

  function flush(): Promise<void> {
    return new Promise((resolve) => setTimeout(resolve, 0));
  }

  beforeEach(() => {
    vi.clearAllMocks();
    versionUpdates$ = new Subject<any>();
    swUpdateMock = {
      isEnabled: true,
      versionUpdates: versionUpdates$.asObservable(),
      checkForUpdate: vi.fn().mockResolvedValue(true),
      activateUpdate: vi.fn().mockResolvedValue(undefined)
    };
    swalMock = {
      confirm: vi.fn().mockResolvedValue({ isConfirmed: false })
    };
    delete (globalThis as any).fetch;
  });

  afterEach(() => {
    (globalThis as any).fetch = originalFetch;
    vi.resetAllMocks();
  });

  it("should read the served SW hash from ngsw.json on init", async () => {
    (globalThis as any).fetch = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({ hash: "abcdef1234567890" })
    });
    const service = createService();

    service.init();
    await flush();

    expect(service.swVersion()).toBe("abcdef1");
    service.ngOnDestroy();
  });

  it("should do nothing when the service worker is disabled", async () => {
    swUpdateMock.isEnabled = false;
    (globalThis as any).fetch = vi.fn();
    const service = createService();

    service.init();
    service.checkForUpdate();
    await flush();

    expect((globalThis as any).fetch).not.toHaveBeenCalled();
    expect(swUpdateMock.checkForUpdate).not.toHaveBeenCalled();
    expect(service.swVersion()).toBeNull();
  });

  it("should prompt and reload when a new version is ready and confirmed", async () => {
    const service = createService();
    const reloadSpy = vi.spyOn(service as any, "reloadApp").mockImplementation(() => undefined);
    swalMock.confirm.mockResolvedValue({ isConfirmed: true });
    service.init();

    versionUpdates$.next({
      type: "VERSION_READY",
      currentVersion: { hash: "old", appData: undefined },
      latestVersion: { hash: "newhash123456", appData: undefined }
    });
    await flush();

    expect(service.swVersion()).toBe("newhash");
    expect(swalMock.confirm).toHaveBeenCalledWith(
      "Nueva versión disponible",
      expect.any(String),
      "Recargar ahora",
      "Más tarde"
    );
    expect(swUpdateMock.activateUpdate).toHaveBeenCalled();
    expect(reloadSpy).toHaveBeenCalled();
    service.ngOnDestroy();
  });

  it("should not reload when the user postpones the update", async () => {
    const service = createService();
    const reloadSpy = vi.spyOn(service as any, "reloadApp").mockImplementation(() => undefined);
    swalMock.confirm.mockResolvedValue({ isConfirmed: false });
    service.init();

    versionUpdates$.next({
      type: "VERSION_READY",
      currentVersion: { hash: "old", appData: undefined },
      latestVersion: { hash: "newhash123456", appData: undefined }
    });
    await flush();

    expect(swUpdateMock.activateUpdate).not.toHaveBeenCalled();
    expect(reloadSpy).not.toHaveBeenCalled();

    // Vuelve a avisar en el siguiente evento.
    versionUpdates$.next({
      type: "VERSION_READY",
      currentVersion: { hash: "old", appData: undefined },
      latestVersion: { hash: "newhash123456", appData: undefined }
    });
    await flush();

    expect(swalMock.confirm).toHaveBeenCalledTimes(2);
    service.ngOnDestroy();
  });

  it("should check for updates when the tab becomes visible", () => {
    const service = createService();
    service.init();

    Object.defineProperty(document, "visibilityState", { value: "visible", configurable: true });
    document.dispatchEvent(new Event("visibilitychange"));

    expect(swUpdateMock.checkForUpdate).toHaveBeenCalled();
    service.ngOnDestroy();
  });
});
