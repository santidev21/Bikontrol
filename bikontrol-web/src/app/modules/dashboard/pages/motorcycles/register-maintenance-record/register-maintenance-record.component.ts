import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Maintenance, CreateMaintenanceRecordRequest } from '../../../interfaces/maintenance.interface';
import { MaintenanceService } from '../../../service/maintenance.service';
import { MotorcyclesService } from '../../../service/motorcycles.service';
import { SwalService } from '../../../../../shared/services/swal.service';

@Component({
  selector: 'app-register-maintenance-record',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './register-maintenance-record.component.html',
  styleUrl: './register-maintenance-record.component.scss'
})
export class RegisterMaintenanceRecordComponent implements OnInit {
  motorcycleId = '';
  maintenances: Maintenance[] = [];
  selectedMaintenance?: Maintenance;
  currentKm = 0;
  lastMaintenanceKm?: number | null;
  isSubmitting = false;

  form: FormGroup;

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private maintenanceService: MaintenanceService,
    private motorcyclesService: MotorcyclesService,
    private swal: SwalService
  ) {
    this.form = this.fb.group({
      userMaintenanceId: ['', Validators.required],
      performedAt: [this.getTodayDate(), Validators.required],
      performedKm: [null]
    });
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const motorcycleId = params.get('motorcycleId');
      if (!motorcycleId) {
        this.router.navigate(['/dashboard/home']);
        return;
      }

      this.motorcycleId = motorcycleId;
      this.loadData();
    });

    this.form.get('userMaintenanceId')?.valueChanges.subscribe((maintenanceId: string) => {
      this.selectedMaintenance = this.maintenances.find((m) => m.id === maintenanceId);
      this.updateKmControlByTrackingType();
      this.loadLastMaintenanceKm();
    });
  }

  private loadData(): void {
    this.motorcyclesService.getCurrentKm(this.motorcycleId).subscribe({
      next: (res) => {
        this.currentKm = res.km;
      }
    });

    this.maintenanceService.getUserMaintenanceByMotorcycle(this.motorcycleId).subscribe({
      next: (list) => {
        this.maintenances = list;
      },
      error: () => {
        this.swal.error('Error', 'No se pudo cargar los mantenimientos de la moto.');
      }
    });
  }

  private updateKmControlByTrackingType(): void {
    const control = this.form.get('performedKm');
    if (!control) return;

    if (this.selectedMaintenance?.trackingType === 'Km') {
      control.setValidators([Validators.required, Validators.min(1)]);
      control.setValue(this.currentKm);
    } else {
      control.clearValidators();
      control.setValue(null);
    }

    control.updateValueAndValidity();
  }

  private loadLastMaintenanceKm(): void {
    this.lastMaintenanceKm = null;
    const maintenanceId = this.form.get('userMaintenanceId')?.value;
    if (!maintenanceId) return;

    this.maintenanceService.getMaintenanceRecordsByMotorcycle(this.motorcycleId).subscribe({
      next: (records) => {
        const last = records.find((x) => x.userMaintenanceId === maintenanceId);
        this.lastMaintenanceKm = last?.performedKm ?? null;
      }
    });
  }

  onSubmit(): void {
    if (this.form.invalid || !this.selectedMaintenance) {
      this.form.markAllAsTouched();
      return;
    }

    const performedAt = new Date(this.form.get('performedAt')?.value as string);
    const today = new Date(this.getTodayDate());

    if (performedAt > today) {
      this.swal.warning('Error', 'No puedes agregar mantenimientos posteriores al dia de hoy');
      return;
    }

    const performedKm = this.form.get('performedKm')?.value as number | null;
    if (
      this.selectedMaintenance.trackingType === 'Km' &&
      this.lastMaintenanceKm != null &&
      performedKm != null &&
      performedKm < this.lastMaintenanceKm
    ) {
      this.swal.warning('Error', 'No puedes agregar mantenimiento anterior al ultimo');
      return;
    }

    const payload: CreateMaintenanceRecordRequest = {
      motorcycleId: this.motorcycleId,
      userMaintenanceId: this.form.get('userMaintenanceId')?.value,
      performedAt: performedAt.toISOString(),
      performedKm: this.selectedMaintenance.trackingType === 'Km' ? performedKm : null
    };

    this.isSubmitting = true;
    this.maintenanceService.registerMaintenanceRecord(payload).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.swal.success('¡Éxito!', 'Se registró el mantenimiento correctamente.').then(() => {
          this.router.navigate(['/dashboard/motorcycles/summary'], {
            queryParams: { motorcycleId: this.motorcycleId }
          });
        });
      },
      error: (err) => {
        this.isSubmitting = false;
        this.swal.error('Error', err?.error?.error || 'No se pudo registrar el mantenimiento.');
      }
    });
  }

  private getTodayDate(): string {
    return new Date().toISOString().split('T')[0];
  }
}
