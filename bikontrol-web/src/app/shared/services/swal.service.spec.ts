import { SwalService } from "./swal.service";
import Swal from "sweetalert2";

jest.mock("sweetalert2", () => ({ fire: jest.fn().mockResolvedValue({ isConfirmed: true }) }));

describe("SwalService", () => {
  let service: SwalService;
  beforeEach(() => { service = new SwalService(); jest.clearAllMocks(); });

  it("success should call Swal.fire with success icon", async () => {
    await service.success("Title", "Text");
    expect(Swal.fire).toHaveBeenCalledWith(expect.objectContaining({ icon: "success", title: "Title", text: "Text" }));
  });
  it("error should call Swal.fire with error icon", async () => {
    await service.error("E", "T");
    expect(Swal.fire).toHaveBeenCalledWith(expect.objectContaining({ icon: "error" }));
  });
  it("warning should call Swal.fire with warning icon", async () => {
    await service.warning("W", "T");
    expect(Swal.fire).toHaveBeenCalledWith(expect.objectContaining({ icon: "warning" }));
  });
  it("confirm should call Swal.fire with showCancelButton", async () => {
    await service.confirm("C", "T");
    expect(Swal.fire).toHaveBeenCalledWith(expect.objectContaining({ showCancelButton: true }));
  });
});
