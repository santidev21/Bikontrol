import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { environment } from '@env/environment';
import { AuthService } from '../../auth/services/auth.service';
import { HttpErrorService } from '../../../shared/services/http-error.service';
import { I18nService } from '../../../shared/i18n/i18n.service';
import { TranslatePipe } from '../../../shared/i18n/translate.pipe';
import { LanguageSwitcherComponent } from '../../../shared/i18n/language-switcher.component';

/**
 * Public marketing landing (root path for guests). Value proposition, how it
 * works and CTAs; authenticated users are sent to the dashboard by
 * `rootRedirectGuard`. Texts come from the i18n dictionaries. Real screenshots
 * are pending (see docs/specs/landing.md).
 */
@Component({
  selector: 'app-landing',
  imports: [RouterLink, TranslatePipe, LanguageSwitcherComponent],
  templateUrl: './landing.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './landing.component.scss',
})
export class LandingComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly httpError = inject(HttpErrorService);
  private readonly i18n = inject(I18nService);

  readonly demoEnabled = environment.demoEnabled;
  readonly demoLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly features = [
    { titleKey: 'landing.feature.reminders.title', descKey: 'landing.feature.reminders.desc' },
    { titleKey: 'landing.feature.history.title', descKey: 'landing.feature.history.desc' },
    { titleKey: 'landing.feature.costs.title', descKey: 'landing.feature.costs.desc' },
    { titleKey: 'landing.feature.data.title', descKey: 'landing.feature.data.desc' },
  ];

  readonly steps = [
    { titleKey: 'landing.step.account.title', descKey: 'landing.step.account.desc' },
    { titleKey: 'landing.step.plan.title', descKey: 'landing.step.plan.desc' },
    { titleKey: 'landing.step.alerts.title', descKey: 'landing.step.alerts.desc' },
  ];

  onDemoLogin(): void {
    if (!this.demoEnabled || this.demoLoading()) return;
    this.demoLoading.set(true);
    this.errorMessage.set(null);
    this.authService.demoLogin().subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: (error) => {
        this.demoLoading.set(false);
        this.errorMessage.set(this.httpError.message(error, this.i18n.t('landing.demoError')));
      },
    });
  }
}
