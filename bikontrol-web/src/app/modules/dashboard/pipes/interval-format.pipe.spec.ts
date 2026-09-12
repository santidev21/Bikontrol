import { IntervalFormatPipe } from "./interval-format.pipe";

describe("IntervalFormatPipe", () => {
  let pipe: IntervalFormatPipe;
  beforeEach(() => { pipe = new IntervalFormatPipe(); });

  it("should return N/A for null/0/negative", () => {
    expect(pipe.transform(null)).toBe("N/A");
    expect(pipe.transform(0)).toBe("N/A");
    expect(pipe.transform(-5)).toBe("N/A");
  });
  it("should return Semanal for 1 week", () => { expect(pipe.transform(1)).toBe("Semanal"); });
  it("should return 15 dias for 2 weeks", () => { expect(pipe.transform(2)).toBe("15 dias"); });
  it("should return 3 semanas for 3 weeks", () => { expect(pipe.transform(3)).toBe("3 semanas"); });
  it("should return months for >=4 weeks", () => {
    expect(pipe.transform(4)).toBe("1 meses");
    expect(pipe.transform(8)).toBe("2 meses");
    expect(pipe.transform(6)).toBe("1.5 meses");
  });
});
