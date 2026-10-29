import { Component, ChangeDetectionStrategy, signal, inject } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { HttpErrorService } from '../../../../shared/services/http-error.service';
import { TranslatePipe } from '../../../../shared/i18n/translate.pipe';
import { SpinnerComponent } from '../../../../shared/components/spinner/spinner.component';

type UnsubscribeState = 'ready' | 'loading' | 'done' | 'error';

/**
 * Landing page of the "unsubscribe" link in reminder emails. It never acts on
 * load (mail scanners and link previewers open links): the user confirms with a
 * button, which POSTs the signed token from the link.
 */
@Component({
  selector: 'app-unsubscribe',
  imports: [RouterModule, TranslatePipe, SpinnerComponent],
  templateUrl: './unsubscribe.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UnsubscribeComponent {
  private route = inject(ActivatedRoute);
  private authService = inject(AuthService);
  private httpError = inject(HttpErrorService);

  private readonly token = this.route.snapshot.queryParamMap.get('token');

  readonly hasToken = !!this.token;
  readonly state = signal<UnsubscribeState>('ready');
  readonly errorMessage = signal<string | null>(null);

  confirm(): void {
    if (!this.token || this.state() === 'loading') return;

    this.state.set('loading');
    this.authService.unsubscribeReminderEmails(this.token).subscribe({
      next: () => this.state.set('done'),
      error: (error) => {
        this.errorMessage.set(this.httpError.message(error));
        this.state.set('error');
      },
    });
  }
}
