import { AfterViewInit, Component } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AUTH_IMPORTS } from '../../auth-imports';
import { HttpErrorService } from '../../../../shared/services/http-error.service';
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
    styleUrl: './login.component.scss'
})
export class LoginComponent implements AfterViewInit {
  loginForm: FormGroup;
  submitted = false;
  errorMessage: string | null = null;
  demoLoading = false;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private httpError: HttpErrorService
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]]
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
    const control = this.loginForm.get(controlName);
    return !!(control && control.invalid && (control.touched || control.dirty || this.submitted));
  }

  onSubmit() {
    this.submitted = true;
    this.errorMessage = null;

    if (this.loginForm.invalid) return;

    const payload = {
      email: this.loginForm.value.email,
      password: this.loginForm.value.password
    };

    this.authService.login(payload.email, payload.password).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: (error) => {
        this.errorMessage = this.httpError.message(error);
      }
    });
  }

  onDemoLogin(): void {
    this.demoLoading = true;
    this.errorMessage = null;
    this.authService.demoLogin().subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: (error) => {
        this.demoLoading = false;
        this.errorMessage = this.httpError.message(error, 'No se pudo iniciar la demo.');
      }
    });
  }

  private renderGoogleButton(): void {
    if (!window.google?.accounts?.id) return;

    window.google.accounts.id.initialize({
      client_id: environment.googleClientId,
      callback: (response: { credential?: string }) => this.onGoogleCredential(response)
    });

    const element = document.getElementById('google-button');
    if (element) {
      window.google.accounts.id.renderButton(element, {
        theme: 'outline',
        size: 'large',
        width: 280,
        shape: 'rectangular'
      });
    }
  }

  onGoogleCredential(response: { credential?: string }): void {
    if (!response?.credential) {
      this.errorMessage = 'No se pudo obtener la credencial de Google.';
      return;
    }

    this.authService.googleLogin(response.credential).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: (error) => {
        this.errorMessage = this.httpError.message(error);
      }
    });
  }
}