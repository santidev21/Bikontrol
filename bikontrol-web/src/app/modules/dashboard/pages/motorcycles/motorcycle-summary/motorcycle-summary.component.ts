import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Motorcycle } from '../../../interfaces/motorcycle.interface';
import { MaintenanceRecord, UpcomingMaintenance } from '../../../interfaces/maintenance.interface';
import { MaintenanceService } from '../../../service/maintenance.service';
import { MotorcyclesService } from '../../../service/motorcycles.service';
import { SwalService } from '../../../../../shared/services/swal.service';

@Component({
  selector: 'app-motorcycle-summary',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './motorcycle-summary.component.html',
  styleUrl: './motorcycle-summary.component.scss'
})
export class MotorcycleSummaryComponent implements OnInit {
  motorcycle?: Motorcycle;
  upcomingMaintenances: UpcomingMaintenance[] = [];
  maintenanceRecords: MaintenanceRecord[] = [];
  currentKm = 0;
  editableKm = 0;
  isEditKmModalOpen = false;
  isSubmittingKm = false;
  isRollingBackKm = false;

  get canRegisterMaintenance(): boolean {
    return this.upcomingMaintenances.length > 0;
  }

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private maintenanceService: MaintenanceService,
    private motorcyclesService: MotorcyclesService,
    private swal: SwalService
  ) {}

  ngOnInit(): void {
    const navState = this.router.getCurrentNavigation()?.extras?.state as { motorcycle?: Motorcycle };
    this.motorcycle = navState?.motorcycle ?? (history.state as { motorcycle?: Motorcycle })?.motorcycle;

    const motorcycleIdFromQuery = this.route.snapshot.queryParamMap.get('motorcycleId');
    if (motorcycleIdFromQuery && !this.motorcycle?.id) {
      this.motorcyclesService.getById(motorcycleIdFromQuery).subscribe({
        next: (motorcycle) => {
          this.motorcycle = motorcycle;
          this.loadSummaryData();
        },
        error: () => this.router.navigate(['/dashboard/home'])
      });
      return;
    }

    if (!this.motorcycle) {
      setTimeout(() => {
        this.router.navigate(['/dashboard/home']);
      }, 1500);
    } else {
      this.loadSummaryData();
    }
  }

  loadSummaryData(): void {
    if (!this.motorcycle?.id) return;

    this.motorcyclesService.getCurrentKm(this.motorcycle.id).subscribe({
      next: (res) => {
        this.currentKm = res.km;
        if (!this.isEditKmModalOpen) {
          this.editableKm = res.km;
        }
      }
    });

    this.maintenanceService.getUpcomingByMotorcycle(this.motorcycle.id).subscribe({
      next: (res) => {
        this.upcomingMaintenances = res;
      },
      error: (err) => {
        console.error('Error loading upcoming maintenances:', err);
      }
    });

    this.maintenanceService.getMaintenanceRecordsByMotorcycle(this.motorcycle.id).subscribe({
      next: (res) => {
        this.maintenanceRecords = res;
      },
      error: (err) => {
        console.error('Error loading maintenance records:', err);
      }
    });
  }

  getStrokeDashoffset(percent: number): number {
    const radius = 24;
    const circumference = 2 * Math.PI * radius;
    return circumference - (Math.max(0, Math.min(100, percent)) / 100) * circumference;
  }

  goToRegisterMaintenanceRecord(): void {
    if (!this.motorcycle?.id || !this.canRegisterMaintenance) return;

    this.router.navigate(['/dashboard/motorcycles', this.motorcycle.id, 'register-maintenance']);
  }

  goToMaintenanceCatalog(): void {
    if (!this.motorcycle?.id) return;

    this.router.navigate(['/dashboard/motorcycles', this.motorcycle.id, 'maintenance']);
  }

  openEditKmModal(): void {
    this.editableKm = this.currentKm;
    this.isEditKmModalOpen = true;
  }

  closeEditKmModal(): void {
    this.isEditKmModalOpen = false;
    this.isSubmittingKm = false;
  }

  saveKm(): void {
    if (!this.motorcycle?.id || this.isSubmittingKm) return;

    if (this.editableKm < this.currentKm) {
      this.swal.error('¡Error!', 'No puede agregar un Kilometraje inferior al actual.');
      return;
    }

    this.isSubmittingKm = true;
    this.motorcyclesService.addKmHistory(this.motorcycle.id, this.editableKm).subscribe({
      next: () => {
        this.closeEditKmModal();
        this.swal.success('¡Exito!.', 'Se ha actualizado el Km de su motocicleta.').then(() => {
          this.loadSummaryData();
        });
      },
      error: (err) => {
        this.isSubmittingKm = false;
        this.swal.error('Error', err?.error?.message || 'No se pudo actualizar el kilometraje.');
      }
    });
  }

  rollbackLastKm(): void {
    if (!this.motorcycle?.id || this.isRollingBackKm) return;

    this.swal.confirm(
      'Confirmar reversión',
      'Esto revertirá únicamente el último cambio de kilometraje.',
      'Revertir',
      'Cancelar'
    ).then((result) => {
      if (!result.isConfirmed) return;

      this.isRollingBackKm = true;
      this.motorcyclesService.rollbackLastKm(this.motorcycle!.id!, this.currentKm).subscribe({
        next: () => {
          this.swal.success('¡Éxito!', 'Se revirtió el último cambio de kilometraje.').then(() => {
            this.loadSummaryData();
          });
        },
        error: (err) => {
          this.swal.error('Error', err?.error?.message || 'No se pudo revertir el kilometraje.');
        },
        complete: () => {
          this.isRollingBackKm = false;
        }
      });
    });
  }
}
