import { Component, OnInit, ChangeDetectionStrategy, signal, inject } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { HttpErrorService } from '../../../../shared/services/http-error.service';

@Component({
  selector: 'app-confirm-email',
  imports: [RouterModule],
  templateUrl: './confirm-email.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './confirm-email.component.scss',
})
export class ConfirmEmailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private authService = inject(AuthService);
  private httpError = inject(HttpErrorService);

  readonly loading = signal(true);
  readonly success = signal(false);
  readonly successMessage = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    const token = this.route.snapshot.queryParamMap.get('token');
    const email = this.route.snapshot.queryParamMap.get('email');

    if (!token || !email) {
      this.loading.set(false);
      this.errorMessage.set('El enlace de confirmación es inválido o está incompleto.');
      return;
    }

    this.authService.confirmEmail(email, token).subscribe({
      next: (response) => {
        this.loading.set(false);
        this.success.set(true);
        this.successMessage.set(response.message);
      },
      error: (error) => {
        this.loading.set(false);
        this.errorMessage.set(this.httpError.message(error));
      },
    });
  }
}
