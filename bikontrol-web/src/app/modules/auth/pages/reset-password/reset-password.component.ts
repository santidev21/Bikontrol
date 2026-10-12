import {
  Component,
  OnDestroy,
  OnInit,
  ChangeDetectionStrategy,
  signal,
  inject,
} from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { HttpErrorService } from '../../../../shared/services/http-error.service';
import { isInvalid as formIsInvalid } from '../../../../shared/utils/form.utils';
import { TranslatePipe } from '../../../../shared/i18n/translate.pipe';
import { I18nService } from '../../../../shared/i18n/i18n.service';
import { SpinnerComponent } from '../../../../shared/components/spinner/spinner.component';

@Component({
  selector: 'app-reset-password',
  imports: [FormsModule, ReactiveFormsModule, RouterModule, TranslatePipe, SpinnerComponent],
  templateUrl: './reset-password.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './reset-password.component.scss',
})
export class ResetPasswordComponent implements OnInit, OnDestroy {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private authService = inject(AuthService);
  private router = inject(Router);
  private httpError = inject(HttpErrorService);
  private i18n = inject(I18nService);

  form: FormGroup;
  readonly submitted = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly linkInvalid = signal(false);
  readonly isSubmitting = signal(false);

  private token: string | null = null;
  private email: string | null = null;
  private redirectTimer?: ReturnType<typeof setTimeout>;

  constructor() {
    this.form = this.fb.group(
      {
        newPassword: ['', [Validators.required, Validators.minLength(6)]],
        confirmPassword: ['', Validators.required],
      },
      { validators: this.passwordMatchValidator },
    );
  }

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token');
    this.email = this.route.snapshot.queryParamMap.get('email');
    this.linkInvalid.set(!this.token || !this.email);
  }

  get f() {
    return this.form.controls;
  }

  isInvalid(controlName: string): boolean {
    return formIsInvalid(this.form, controlName, this.submitted());
  }

  passwordMatchValidator(form: FormGroup) {
    const pass = form.get('newPassword')?.value;
    const confirm = form.get('confirmPassword')?.value;
    return pass === confirm ? null : { passwordMismatch: true };
  }

  onSubmit() {
    this.submitted.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    if (this.form.invalid || this.linkInvalid()) return;

    this.isSubmitting.set(true);
    this.authService
      .resetPassword(this.email!, this.token!, this.form.value.newPassword)
      .subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.successMessage.set(this.i18n.t('auth.reset.success'));
          this.redirectTimer = setTimeout(() => this.router.navigate(['/login']), 2000);
        },
        error: (error) => {
          this.isSubmitting.set(false);
          this.errorMessage.set(this.httpError.message(error));
        },
      });
  }

  ngOnDestroy(): void {
    if (this.redirectTimer) {
      clearTimeout(this.redirectTimer);
    }
  }
}
