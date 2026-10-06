import { Component, OnInit, ChangeDetectionStrategy, signal, inject } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { HttpErrorService } from '../../../../shared/services/http-error.service';
import { TranslatePipe } from '../../../../shared/i18n/translate.pipe';
import { I18nService } from '../../../../shared/i18n/i18n.service';

@Component({
  selector: 'app-confirm-email',
  imports: [RouterModule, TranslatePipe],
  templateUrl: './confirm-email.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './confirm-email.component.scss',
})
export class ConfirmEmailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private authService = inject(AuthService);
  private httpError = inject(HttpErrorService);
  private i18n = inject(I18nService);

  readonly loading = signal(true);
  readonly success = signal(false);
  readonly successMessage = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    const token = this.route.snapshot.queryParamMap.get('token');
    const email = this.route.snapshot.queryParamMap.get('email');

    if (!token || !email) {
      this.loading.set(false);
      this.errorMessage.set(this.i18n.t('auth.confirm.linkInvalid'));
      return;
    }

    this.authService.confirmEmail(email, token).subscribe({
      next: () => {
        this.loading.set(false);
        this.success.set(true);
        this.successMessage.set(this.i18n.t('auth.confirm.success'));
      },
      error: (error) => {
        this.loading.set(false);
        this.errorMessage.set(this.httpError.message(error));
      },
    });
  }
}
