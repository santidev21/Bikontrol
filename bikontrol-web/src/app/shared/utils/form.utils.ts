import { FormGroup } from '@angular/forms';

/** True when the control is touched and currently fails the given validation error. */
export function hasError(form: FormGroup, field: string, type: string): boolean {
  const control = form.get(field);
  return !!control && control.hasError(type) && control.touched;
}

/**
 * True when the control is invalid and the user should already see the error
 * (touched, dirty, or a submit attempt happened).
 */
export function isInvalid(form: FormGroup, controlName: string, submitted: boolean): boolean {
  const control = form.get(controlName);
  return !!(control && control.invalid && (control.touched || control.dirty || submitted));
}
