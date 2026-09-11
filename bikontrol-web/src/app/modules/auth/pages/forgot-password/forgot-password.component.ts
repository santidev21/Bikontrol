import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { HttpErrorService } from '../../../../shared/services/http-error.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.scss'
})
export class ForgotPasswordComponent {
  form: FormGroup;
  submitted = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

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
    return !!(control && control.invalid && (control.touched || control.dirty || this.submitted));
  }

  onSubmit() {
    this.submitted = true;
    this.errorMessage = null;
    this.successMessage = null;

    if (this.form.invalid) return;

    this.authService.forgotPassword(this.form.value.email).subscribe({
      next: (response) => {
        this.successMessage = response.message;
      },
      error: (error) => {
        this.errorMessage = this.httpError.message(error);
      }
    });
  }
}