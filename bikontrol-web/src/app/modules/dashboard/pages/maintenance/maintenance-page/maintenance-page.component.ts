import { ChangeDetectionStrategy, Component, OnInit, computed, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MaintenanceService } from '../../../service/maintenance.service';
import { MaintenanceInfoCardComponent } from "../../../components/maintenance-info-card/maintenance-info-card.component";
import { SwalService } from '../../../../../shared/services/swal.service';
import { HttpErrorService } from '../../../../../shared/services/http-error.service';
import { AuthService } from '../../../../auth/services/auth.service';

@Component({
    selector: 'app-maintenance-page',
    imports: [RouterModule, MaintenanceInfoCardComponent],
    templateUrl: './maintenance-page.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrl: './maintenance-page.component.scss'
})
export class MaintenancePageComponent implements OnInit {
  private readonly maintenanceService = inject(MaintenanceService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly swal = inject(SwalService);
  private readonly httpError = inject(HttpErrorService);
  private readonly authService = inject(AuthService);

  readonly motorcycleId = signal('');
  readonly isDemo = computed(() => this.authService.isDemo());

  private readonly userMaintenanceId = computed(() => this.motorcycleId() || undefined);

  private readonly userRes = this.maintenanceService.getUserMaintenanceByMotorcycleResource(this.userMaintenanceId);
  private readonly defaultsRes = this.maintenanceService.getDefaultsResource();

  readonly userMaintenance = computed(() => (this.userRes.hasValue() ? this.userRes.value()! : []));
  readonly defaultMaintenance = computed(() => (this.defaultsRes.hasValue() ? this.defaultsRes.value()! : []));

  constructor() {
    effect(() => {
      const error = this.userRes.error();
      if (error) {
        this.swal.error('Error', this.httpError.message(error, 'No se pudieron cargar tus mantenimientos.'));
      }
    });

    effect(() => {
      const error = this.defaultsRes.error();
      if (error) {
        this.swal.error('Error', this.httpError.message(error, 'No se pudieron cargar los mantenimientos predeterminados.'));
      }
    });
  }

  ngOnInit(): void {
    const motorcycleId = this.route.snapshot.paramMap.get('motorcycleId');
    if (!motorcycleId) {
      this.swal.warning('Contexto requerido', 'Primero selecciona una motocicleta para gestionar mantenimientos.');
      this.router.navigate(['/dashboard/home']);
      return;
    }

    this.motorcycleId.set(motorcycleId);
  }

  reload(): void {
    this.userRes.reload();
    this.defaultsRes.reload();
  }

  goToAddMaintenance(): void {
    this.router.navigate(['/dashboard/motorcycles', this.motorcycleId(), 'maintenance/add']);
  }
}
