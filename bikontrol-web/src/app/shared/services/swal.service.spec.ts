import Swal from "sweetalert2";
import { SwalService } from "./swal.service";

describe("SwalService", () => {
  let service: SwalService;
  let fireSpy: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    fireSpy = vi.spyOn(Swal, "fire").mockResolvedValue({ isConfirmed: true } as any);
    service = new SwalService();
  });

  afterEach(() => vi.restoreAllMocks());

  it("success should call Swal.fire with success icon", async () => {
    await service.success("Title", "Text");
    expect(fireSpy).toHaveBeenCalledWith(expect.objectContaining({ icon: "success", title: "Title", text: "Text" }));
  });

  it("error should call Swal.fire with error icon", async () => {
    await service.error("E", "T");
    expect(fireSpy).toHaveBeenCalledWith(expect.objectContaining({ icon: "error" }));
  });

  it("warning should call Swal.fire with warning icon", async () => {
    await service.warning("W", "T");
    expect(fireSpy).toHaveBeenCalledWith(expect.objectContaining({ icon: "warning" }));
  });

  it("confirm should call Swal.fire with showCancelButton", async () => {
    await service.confirm("C", "T");
    expect(fireSpy).toHaveBeenCalledWith(expect.objectContaining({ showCancelButton: true }));
  });
});
