import { Component, OnDestroy, ChangeDetectionStrategy, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MaintenanceService } from '../../../service/maintenance.service';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { SwalService } from '../../../../../shared/services/swal.service';
import { HttpErrorService } from '../../../../../shared/services/http-error.service';

import { SaveMaintenanceDTO } from '../../../interfaces/maintenance.interface';
import { MonitoringTypeSelectorComponent } from '../components/monitoring-type-selector/monitoring-type-selector.component';

@Component({
    selector: 'app-save-maintenance',
    imports: [ReactiveFormsModule, MonitoringTypeSelectorComponent],
    templateUrl: './save-maintenance.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrl: './save-maintenance.component.scss'
})
export class SaveMaintenanceComponent implements OnDestroy {
  maintenanceForm: FormGroup;
  readonly isSubmitting = signal(false);
  readonly isEditMode = signal(false);
  readonly maintenanceId = signal<string | undefined>(undefined);
  readonly motorcycleId = signal('');

  private readonly subscriptions = new Subscription();

  constructor(
    private fb: FormBuilder,
    private maintenanceService: MaintenanceService,
    private router: Router,
    private route: ActivatedRoute,
    private swal: SwalService,
    private httpError: HttpErrorService
  ) {
    this.maintenanceForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      description: ['', [Validators.required]],
      monitoringType: ['km', [Validators.required]],
      kmInterval: [1, [Validators.required, Validators.min(1), Validators.max(1000000)]],
      timeIntervalWeeks: [1, [Validators.required, Validators.min(1), Validators.max(520)]],
      timeIntervalUnit: ['weeks'],
    });
  }

  ngOnInit(): void {
    const motorcycleId = this.route.snapshot.paramMap.get('motorcycleId');
    if (motorcycleId) {
      this.motorcycleId.set(motorcycleId);
    }

    this.subscriptions.add(
      this.route.paramMap.subscribe((params) => {
        const id = params.get('id');
        if (id) {
          this.isEditMode.set(true);
          this.maintenanceId.set(id);
          this.loadMaintenanceId(id);
        }
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  loadMaintenanceId(id: string): void {
    this.maintenanceService.getById(id).subscribe({
      next: (maintenance) => {
        const mappedMaintenance = {
          ...maintenance,
          monitoringType: maintenance.trackingType === 'Km' ? 'km' : 'time'
        };

        if (!this.motorcycleId() && maintenance.motorcycleId) {
          this.motorcycleId.set(maintenance.motorcycleId);
        }

        this.maintenanceForm.patchValue(mappedMaintenance);
        this.maintenanceForm.get('monitoringType')?.disable();
      },
      error: (err) => {
        this.swal.error('Error', this.httpError.message(err, 'No se pudo cargar el mantenimiento.'));
      },
    });
  }

  onSubmit(): void {
    if (this.maintenanceForm.invalid) {
      this.maintenanceForm.markAllAsTouched();
      this.swal.warning('Formulario incompleto', 'Por favor completa todos los campos requeridos.');
      return;
    }

    this.isSubmitting.set(true);
    const maintenance: SaveMaintenanceDTO = this.maintenanceForm.value;
    const monitoringType = this.maintenanceForm.get('monitoringType')?.value;

    maintenance.motorcycleId = this.motorcycleId();
    maintenance.kmInterval = 0;
    maintenance.timeIntervalWeeks = 0;
    maintenance.trackingType = monitoringType === 'km' ? 'Km' : 'Time';

    if (monitoringType === 'km') {
      maintenance.kmInterval = this.maintenanceForm.get('kmInterval')?.value;
    } else {
      const timeValue = this.maintenanceForm.get('timeIntervalWeeks')?.value;
      const timeUnit = this.maintenanceForm.get('timeIntervalUnit')?.value;
      if (timeValue && timeUnit) {
        maintenance.timeIntervalWeeks = this.convertToWeeks(timeValue, timeUnit);
      }
    }

    const id = this.maintenanceId();
    if (this.isEditMode() && id) {
      this.updateMaintenance(id, maintenance);
    } else {
      this.addMaintenance(maintenance);
    }
  }

  private convertToWeeks(value: number, unit: 'weeks' | 'months' | 'years'): number {
    switch (unit) {
      case 'weeks':
        return value;
      case 'months':
        return value * 4;
      case 'years':
        return value * 52;
      default:
        return value;
    }
  }

  addMaintenance(maintenance: SaveMaintenanceDTO): void {
    if (!maintenance.motorcycleId) {
      this.swal.error('Error', 'Debes seleccionar una motocicleta para crear el mantenimiento.');
      this.isSubmitting.set(false);
      return;
    }

    this.maintenanceService.createUserMaintenance(maintenance).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.swal.success('Éxito', 'Mantenimiento agregado correctamente.').then(() => {
          this.router.navigate(['/dashboard/motorcycles', this.motorcycleId(), 'maintenance']);
        });
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.swal.error('Error', this.httpError.message(err, 'No se pudo agregar el mantenimiento.'));
      },
    });
  }

  updateMaintenance(id: string, maintenance: SaveMaintenanceDTO): void {
    this.maintenanceService.updateMaintenance(id, maintenance).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.swal.success('Éxito', 'Mantenimiento actualizado correctamente.').then(() => {
          this.router.navigate(['/dashboard/motorcycles', this.motorcycleId() || maintenance.motorcycleId, 'maintenance']);
        });
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.swal.error('Error', this.httpError.message(err, 'No se pudo actualizar el mantenimiento.'));
      },
    });
  }

  hasError(field: string, type: string): boolean {
    const control = this.maintenanceForm.get(field);
    return !!control && control.hasError(type) && control.touched;
  }
}
