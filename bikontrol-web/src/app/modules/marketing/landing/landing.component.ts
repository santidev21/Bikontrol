import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { environment } from '@env/environment';
import { AuthService } from '../../auth/services/auth.service';
import { HttpErrorService } from '../../../shared/services/http-error.service';

/**
 * Public marketing landing (root path for guests). Value proposition, how it
 * works and CTAs; authenticated users are sent to the dashboard by
 * `rootRedirectGuard`. Real screenshots are pending (see docs/specs/landing.md).
 */
@Component({
  selector: 'app-landing',
  imports: [RouterLink],
  templateUrl: './landing.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './landing.component.scss',
})
export class LandingComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly httpError = inject(HttpErrorService);

  readonly demoEnabled = environment.demoEnabled;
  readonly demoLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly features = [
    {
      title: 'No te olvides más del service',
      description: 'Recordatorios por kilometraje o por tiempo, antes de que venza.',
    },
    {
      title: 'Todo tu historial en un lugar',
      description: 'Qué le hiciste, cuándo y a qué kilometraje, siempre a mano.',
    },
    {
      title: 'Sabés cuánto gastás',
      description: 'Costo por moto, por año y por kilómetro.',
    },
    {
      title: 'Tus datos, tuyos',
      description: 'Descargá todo cuando quieras y borrá tu cuenta en un clic.',
    },
  ];

  readonly steps = [
    { title: 'Creá tu cuenta', description: 'En un minuto, con tu correo o con Google.' },
    {
      title: 'Cargá tu moto y tu plan',
      description: 'Elegí tus mantenimientos; te sugerimos los esenciales.',
    },
    {
      title: 'Recibí los avisos',
      description: 'Por email y notificaciones, antes de que toque el service.',
    },
  ];

  onDemoLogin(): void {
    if (!this.demoEnabled || this.demoLoading()) return;
    this.demoLoading.set(true);
    this.errorMessage.set(null);
    this.authService.demoLogin().subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: (error) => {
        this.demoLoading.set(false);
        this.errorMessage.set(this.httpError.message(error, 'No se pudo iniciar la demo.'));
      },
    });
  }
}
