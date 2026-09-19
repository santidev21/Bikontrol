
import { Component, ChangeDetectionStrategy, signal } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { HttpErrorService } from '../../../../shared/services/http-error.service';

@Component({
    selector: 'app-forgot-password',
    imports: [FormsModule, ReactiveFormsModule, RouterModule],
    templateUrl: './forgot-password.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrl: './forgot-password.component.scss'
})
export class ForgotPasswordComponent {
  form: FormGroup;
  readonly submitted = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private httpError: HttpErrorService
  ) {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });
  }

  get f() {
    return this.form.controls;
  }

  isInvalid(controlName: string): boolean {
    const control = this.form.get(controlName);
    return !!(control && control.invalid && (control.touched || control.dirty || this.submitted()));
  }

  onSubmit() {
    this.submitted.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    if (this.form.invalid) return;

    this.authService.forgotPassword(this.form.value.email).subscribe({
      next: (response) => {
        this.successMessage.set(response.message);
      },
      error: (error) => {
        this.errorMessage.set(this.httpError.message(error));
      }
    });
  }
}