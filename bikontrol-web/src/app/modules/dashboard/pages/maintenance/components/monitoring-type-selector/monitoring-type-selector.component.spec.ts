import { FormBuilder, Validators } from "@angular/forms";
import { MonitoringTypeSelectorComponent } from "./monitoring-type-selector.component";

describe("MonitoringTypeSelectorComponent", () => {
  let component: MonitoringTypeSelectorComponent;
  const fb = new FormBuilder();
  beforeEach(() => { component = new MonitoringTypeSelectorComponent(); });

  it("should return false when form is missing", () => {
    expect(component.hasError("kmInterval", "required")).toBeFalsy();
  });

  it("should validate kmInterval only when monitoringType is km", () => {
    const form = fb.group({
      monitoringType: ["time"],
      kmInterval: ["", Validators.required],
      timeIntervalWeeks: ["", Validators.required],
      timeIntervalUnit: ["weeks"]
    });
    form.get("kmInterval")!.markAsTouched();
    component.form = form;
    expect(component.hasError("kmInterval", "required")).toBeFalsy();
    form.get("monitoringType")!.setValue("km");
    expect(component.hasError("kmInterval", "required")).toBeTruthy();
  });

  it("should validate time fields only when monitoringType is time", () => {
    const form = fb.group({
      monitoringType: ["km"],
      kmInterval: ["1"],
      timeIntervalWeeks: ["", Validators.required],
      timeIntervalUnit: ["weeks"]
    });
    form.get("timeIntervalWeeks")!.markAsTouched();
    component.form = form;
    expect(component.hasError("timeIntervalWeeks", "required")).toBeFalsy();
    form.get("monitoringType")!.setValue("time");
    expect(component.hasError("timeIntervalWeeks", "required")).toBeTruthy();
  });
});
