import { Component } from '@angular/core';
import { MyErrorStateMatcher } from '../register/register.component';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AuthService } from '../../auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {

  matcher = new MyErrorStateMatcher();

  loginForm: FormGroup;
  hide: boolean = true; // For password visibility toggle

  constructor(private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
  }

  login() {
    if (this.loginForm.valid) {
      const formData = this.loginForm.value;
      this.authService.login(formData).subscribe({
        next: (response) => {
        console.log('Login successful:', response);

        localStorage.setItem('token', response.token);
        localStorage.setItem('userId', response.userId);
        localStorage.setItem('fullName', response.fullName);
        localStorage.setItem('expiresAt', response.expiresAt);

        this.router.navigate(['/dashboard']);
      },
        error: (err) => console.error('Login failed:', err.message)
      });
    } else {
      this.loginForm.markAllAsTouched();
    }
  }

  get email() {
    return this.loginForm.get('email')!;
  }

  get password() {
    return this.loginForm.get('password')!;
  }
}
