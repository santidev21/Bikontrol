import { Component, ChangeDetectionStrategy, signal, inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';
import { AUTH_IMPORTS } from '../../auth-imports';
import { HttpErrorService } from '../../../../shared/services/http-error.service';
import { isInvalid as formIsInvalid } from '../../../../shared/utils/form.utils';
import { I18nService } from '../../../../shared/i18n/i18n.service';

@Component({
  selector: 'app-register',
  imports: [AUTH_IMPORTS],
  templateUrl: './register.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './register.component.scss',
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private httpError = inject(HttpErrorService);
  private i18n = inject(I18nService);

  registerForm: FormGroup;
  readonly submitted = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly confirmationSent = signal(false);
  readonly resendMessage = signal<string | null>(null);
  readonly resendLoading = signal(false);
  private registeredEmail = '';

  constructor() {
    this.registerForm = this.fb.group(
      {
        fullName: ['', Validators.required],
        email: ['', [Validators.required, Validators.email]],
        password: ['', [Validators.required, Validators.minLength(6)]],
        confirmPassword: ['', Validators.required],
      },
      { validators: this.passwordMatchValidator },
    );
  }

  get f() {
    return this.registerForm.controls;
  }

  isInvalid(controlName: string): boolean {
    return formIsInvalid(this.registerForm, controlName, this.submitted());
  }

  passwordMatchValidator(form: FormGroup) {
    const pass = form.get('password')?.value;
    const confirm = form.get('confirmPassword')?.value;
    return pass === confirm ? null : { passwordMismatch: true };
  }

  onSubmit() {
    this.submitted.set(true);
    this.errorMessage.set(null);

    if (this.registerForm.invalid) return;

    const payload = {
      fullName: this.registerForm.value.fullName,
      email: this.registerForm.value.email,
      password: this.registerForm.value.password,
    };

    this.authService.register(payload).subscribe({
      next: (response) => {
        if (response.emailConfirmationRequired) {
          // No session until the email is confirmed: show the "check inbox" screen.
          this.registeredEmail = response.email;
          this.confirmationSent.set(true);
          return;
        }
        this.router.navigate(['/dashboard']);
      },
      error: (error) => {
        this.errorMessage.set(this.httpError.message(error));
      },
    });
  }

  onResendConfirmation(): void {
    if (!this.registeredEmail) return;

    this.resendLoading.set(true);
    this.resendMessage.set(null);
    this.authService.resendConfirmation(this.registeredEmail).subscribe({
      next: () => {
        this.resendLoading.set(false);
        this.resendMessage.set(this.i18n.t('auth.login.resendSent'));
      },
      error: (error) => {
        this.resendLoading.set(false);
        this.errorMessage.set(this.httpError.message(error));
      },
    });
  }
}
