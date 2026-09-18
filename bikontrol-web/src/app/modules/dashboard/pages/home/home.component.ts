import { ChangeDetectionStrategy, Component, effect, inject } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MotorcycleCardComponent } from '../../components/motorcycle-card/motorcycle-card.component';
import { AuthService } from '../../../auth/services/auth.service';
import { MotorcyclesService } from '../../service/motorcycles.service';
import { SwalService } from '../../../../shared/services/swal.service';
import { HttpErrorService } from '../../../../shared/services/http-error.service';

@Component({
    selector: 'app-home',
    imports: [MotorcycleCardComponent, RouterModule],
    templateUrl: './home.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrl: './home.component.scss'
})
export class HomeComponent {
  private readonly motorcycleService = inject(MotorcyclesService);
  private readonly swal = inject(SwalService);
  private readonly httpError = inject(HttpErrorService);
  private readonly authService = inject(AuthService);

  readonly motorcycles = this.motorcycleService.getMyMotorcyclesResource();

  constructor() {
    effect(() => {
      const error = this.motorcycles.error();
      if (error) {
        this.swal.error(
          'Error',
          this.httpError.message(error, 'No se pudieron cargar tus motocicletas.')
        );
      }
    });
  }

  get isDemo(): boolean {
    return this.authService.isDemo();
  }
}
