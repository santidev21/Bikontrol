import { Component, ChangeDetectionStrategy, signal, inject } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { HttpErrorService } from '../../../../shared/services/http-error.service';
import { isInvalid as formIsInvalid } from '../../../../shared/utils/form.utils';
import { TranslatePipe } from '../../../../shared/i18n/translate.pipe';
import { I18nService } from '../../../../shared/i18n/i18n.service';

@Component({
  selector: 'app-forgot-password',
  imports: [FormsModule, ReactiveFormsModule, RouterModule, TranslatePipe],
  templateUrl: './forgot-password.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './forgot-password.component.scss',
})
export class ForgotPasswordComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private httpError = inject(HttpErrorService);
  private i18n = inject(I18nService);

  form: FormGroup;
  readonly submitted = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  constructor() {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
    });
  }

  get f() {
    return this.form.controls;
  }

  isInvalid(controlName: string): boolean {
    return formIsInvalid(this.form, controlName, this.submitted());
  }

  onSubmit() {
    this.submitted.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    if (this.form.invalid) return;

    this.authService.forgotPassword(this.form.value.email).subscribe({
      next: () => {
        this.successMessage.set(this.i18n.t('auth.forgot.success'));
      },
      error: (error) => {
        this.errorMessage.set(this.httpError.message(error));
      },
    });
  }
}
