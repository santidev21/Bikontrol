import { AfterViewInit, Component, ChangeDetectionStrategy, signal, inject } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AUTH_IMPORTS } from '../../auth-imports';
import { HttpErrorService } from '../../../../shared/services/http-error.service';
import { isInvalid as formIsInvalid } from '../../../../shared/utils/form.utils';
import { I18nService } from '../../../../shared/i18n/i18n.service';
import { environment } from '@env/environment';

declare global {
  interface Window {
    google?: any;
  }
}

@Component({
  selector: 'app-login',
  imports: [AUTH_IMPORTS],
  templateUrl: './login.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './login.component.scss',
})
export class LoginComponent implements AfterViewInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private httpError = inject(HttpErrorService);
  private i18n = inject(I18nService);

  loginForm: FormGroup;
  readonly submitted = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly demoLoading = signal(false);
  readonly canResendConfirmation = signal(false);
  readonly resendLoading = signal(false);
  readonly resendMessage = signal<string | null>(null);
  readonly demoEnabled = environment.demoEnabled;

  constructor() {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
    });
  }

  ngAfterViewInit(): void {
    if (window.google?.accounts?.id) {
      this.renderGoogleButton();
      return;
    }
    const script = document.createElement('script');
    script.src = 'https://accounts.google.com/gsi/client';
    script.async = true;
    script.defer = true;
    script.onload = () => this.renderGoogleButton();
    document.body.appendChild(script);
  }

  get f() {
    return this.loginForm.controls;
  }

  isInvalid(controlName: string): boolean {
    return formIsInvalid(this.loginForm, controlName, this.submitted());
  }

  onSubmit() {
    this.submitted.set(true);
    this.errorMessage.set(null);
    this.resendMessage.set(null);
    this.canResendConfirmation.set(false);

    if (this.loginForm.invalid) return;

    const payload = {
      email: this.loginForm.value.email,
      password: this.loginForm.value.password,
    };

    this.authService.login(payload.email, payload.password).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: (error) => {
        this.errorMessage.set(this.httpError.message(error));
        // 403 on login means the email still needs to be confirmed.
        this.canResendConfirmation.set(error?.status === 403);
      },
    });
  }

  onResendConfirmation(): void {
    const email = this.loginForm.value.email;
    if (!email) return;

    this.resendLoading.set(true);
    this.resendMessage.set(null);
    this.authService.resendConfirmation(email).subscribe({
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

  onDemoLogin(): void {
    if (!this.demoEnabled) return;

    this.demoLoading.set(true);
    this.errorMessage.set(null);
    this.authService.demoLogin().subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: (error) => {
        this.demoLoading.set(false);
        this.errorMessage.set(this.httpError.message(error, this.i18n.t('auth.login.demoError')));
      },
    });
  }

  private renderGoogleButton(): void {
    if (!window.google?.accounts?.id) return;

    // No client id => Google throws "Missing required parameter: client_id".
    // Skip rendering the button instead of sending users to a Google error page.
    if (!environment.googleClientId) {
      if (typeof console !== 'undefined') {
        console.warn('[auth] Google client id is not configured; hiding Google sign-in.');
      }
      return;
    }

    window.google.accounts.id.initialize({
      client_id: environment.googleClientId,
      callback: (response: { credential?: string }) => this.onGoogleCredential(response),
    });

    const element = document.getElementById('google-button');
    if (element) {
      window.google.accounts.id.renderButton(element, {
        theme: 'outline',
        size: 'large',
        width: 280,
        shape: 'rectangular',
      });
    }
  }

  onGoogleCredential(response: { credential?: string }): void {
    if (!response?.credential) {
      this.errorMessage.set(this.i18n.t('auth.login.googleError'));
      return;
    }

    this.authService.googleLogin(response.credential).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: (error) => {
        this.errorMessage.set(this.httpError.message(error));
      },
    });
  }
}
