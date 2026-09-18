import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Motorcycle } from '../../../interfaces/motorcycle.interface';
import { MaintenanceRecord, UpcomingMaintenance } from '../../../interfaces/maintenance.interface';
import { MaintenanceService } from '../../../service/maintenance.service';
import { MotorcyclesService } from '../../../service/motorcycles.service';
import { SwalService } from '../../../../../shared/services/swal.service';
import { HttpErrorService } from '../../../../../shared/services/http-error.service';
import { AuthService } from '../../../../auth/services/auth.service';

@Component({
    selector: 'app-motorcycle-summary',
    imports: [CommonModule, RouterModule, FormsModule],
    templateUrl: './motorcycle-summary.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrl: './motorcycle-summary.component.scss'
})
export class MotorcycleSummaryComponent implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly maintenanceService = inject(MaintenanceService);
  private readonly motorcyclesService = inject(MotorcyclesService);
  private readonly swal = inject(SwalService);
  private readonly httpError = inject(HttpErrorService);
  private readonly authService = inject(AuthService);

  readonly motorcycle = signal<Motorcycle | undefined>(undefined);
  readonly currentKm = signal(0);
  readonly upcomingMaintenances = signal<UpcomingMaintenance[]>([]);
  readonly maintenanceRecords = signal<MaintenanceRecord[]>([]);
  readonly isEditKmModalOpen = signal(false);
  readonly editableKm = signal(0);
  readonly isSubmittingKm = signal(false);
  readonly isRollingBackKm = signal(false);

  readonly isDemo = computed(() => this.authService.isDemo());
  readonly motorcycleId = computed(() => this.motorcycle()?.id);
  readonly canRegisterMaintenance = computed(() => this.upcomingMaintenances().length > 0);

  private readonly currentKmRes = this.motorcyclesService.getCurrentKmResource(this.motorcycleId);
  private readonly upcomingRes = this.maintenanceService.getUpcomingResource(this.motorcycleId);
  private readonly recordsRes = this.maintenanceService.getRecordsResource(this.motorcycleId);

  private redirectTimer?: ReturnType<typeof setTimeout>;

  constructor() {
    effect(() => {
      if (this.currentKmRes.hasValue()) {
        const km = this.currentKmRes.value()!.km;
        this.currentKm.set(km);
        if (!this.isEditKmModalOpen()) {
          this.editableKm.set(km);
        }
      }
    });

    effect(() => {
      if (this.upcomingRes.hasValue()) {
        this.upcomingMaintenances.set(this.upcomingRes.value()!);
      }
    });

    effect(() => {
      const error = this.upcomingRes.error();
      if (error) {
        this.swal.error('Error', this.httpError.message(error, 'No se pudieron cargar los mantenimientos próximos.'));
      }
    });

    effect(() => {
      if (this.recordsRes.hasValue()) {
        this.maintenanceRecords.set(this.recordsRes.value()!);
      }
    });

    effect(() => {
      const error = this.recordsRes.error();
      if (error) {
        this.swal.error('Error', this.httpError.message(error, 'No se pudieron cargar los registros de mantenimiento.'));
      }
    });
  }

  ngOnInit(): void {
    const navState = this.router.getCurrentNavigation()?.extras?.state as { motorcycle?: Motorcycle };
    this.motorcycle.set(navState?.motorcycle ?? (history.state as { motorcycle?: Motorcycle })?.motorcycle);

    const motorcycleIdFromQuery = this.route.snapshot.queryParamMap.get('motorcycleId');
    if (motorcycleIdFromQuery && !this.motorcycle()?.id) {
      this.motorcyclesService.getById(motorcycleIdFromQuery).subscribe({
        next: (motorcycle) => this.motorcycle.set(motorcycle),
        error: () => this.router.navigate(['/dashboard/home'])
      });
      return;
    }

    if (!this.motorcycle()) {
      this.redirectTimer = setTimeout(() => {
        this.router.navigate(['/dashboard/home']);
      }, 1500);
    }
  }

  ngOnDestroy(): void {
    if (this.redirectTimer) {
      clearTimeout(this.redirectTimer);
    }
  }

  private reloadData(): void {
    this.currentKmRes.reload();
    this.upcomingRes.reload();
    this.recordsRes.reload();
  }

  getStrokeDashoffset(percent: number): number {
    const radius = 24;
    const circumference = 2 * Math.PI * radius;
    return circumference - (Math.max(0, Math.min(100, percent)) / 100) * circumference;
  }

  goToRegisterMaintenanceRecord(): void {
    const id = this.motorcycleId();
    if (!id || !this.canRegisterMaintenance()) return;

    this.router.navigate(['/dashboard/motorcycles', id, 'register-maintenance']);
  }

  goToMaintenanceCatalog(): void {
    const id = this.motorcycleId();
    if (!id) return;

    this.router.navigate(['/dashboard/motorcycles', id, 'maintenance']);
  }

  openEditKmModal(): void {
    this.editableKm.set(this.currentKm());
    this.isEditKmModalOpen.set(true);
  }

  closeEditKmModal(): void {
    this.isEditKmModalOpen.set(false);
    this.isSubmittingKm.set(false);
  }

  saveKm(): void {
    const id = this.motorcycleId();
    if (!id || this.isSubmittingKm()) return;

    if (this.editableKm() < this.currentKm()) {
      this.swal.error('¡Error!', 'No puede agregar un Kilometraje inferior al actual.');
      return;
    }

    this.isSubmittingKm.set(true);
    this.motorcyclesService.addKmHistory(id, this.editableKm()).subscribe({
      next: () => {
        this.closeEditKmModal();
        this.swal.success('¡Exito!.', 'Se ha actualizado el Km de su motocicleta.').then(() => {
          this.reloadData();
        });
      },
      error: (err) => {
        this.isSubmittingKm.set(false);
        this.swal.error('Error', this.httpError.message(err, 'No se pudo actualizar el kilometraje.'));
      }
    });
  }

  rollbackLastKm(): void {
    const id = this.motorcycleId();
    if (!id || this.isRollingBackKm()) return;

    this.swal.confirm(
      'Confirmar reversión',
      'Esto revertirá únicamente el último cambio de kilometraje.',
      'Revertir',
      'Cancelar'
    ).then((result) => {
      if (!result.isConfirmed) return;

      this.isRollingBackKm.set(true);
      this.motorcyclesService.rollbackLastKm(id, this.currentKm()).subscribe({
        next: () => {
          this.swal.success('¡Éxito!', 'Se revirtió el último cambio de kilometraje.').then(() => {
            this.reloadData();
          });
        },
        error: (err) => {
          this.swal.error('Error', this.httpError.message(err, 'No se pudo revertir el kilometraje.'));
        },
        complete: () => {
          this.isRollingBackKm.set(false);
        }
      });
    });
  }
}
