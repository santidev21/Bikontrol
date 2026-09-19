import { ChangeDetectionStrategy, Component, OnInit, inject, input, output, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { FollowMaintenancePayload, Maintenance } from '../../interfaces/maintenance.interface';
import { MaintenanceService } from '../../service/maintenance.service';
import { MonitoringTypeSelectorComponent } from '../../pages/maintenance/components/monitoring-type-selector/monitoring-type-selector.component';
import { SwalService } from '../../../../shared/services/swal.service';
import { HttpErrorService } from '../../../../shared/services/http-error.service';

@Component({
    selector: 'app-follow-maintenance-modal',
    imports: [ReactiveFormsModule, MonitoringTypeSelectorComponent],
    templateUrl: './follow-maintenance-modal.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class FollowMaintenanceModalComponent implements OnInit {
  readonly maintenance = input.required<Maintenance>();
  readonly isDefault = input(false);
  readonly motorcycleIdContext = input<string | undefined>(undefined);

  readonly saved = output<void>();
  readonly closed = output<void>();

  private readonly fb = inject(FormBuilder);
  private readonly maintenanceService = inject(MaintenanceService);
  private readonly swal = inject(SwalService);
  private readonly httpError = inject(HttpErrorService);

  readonly isFollowing = signal(false);
  followForm: FormGroup;

  constructor() {
    this.followForm = this.fb.group({
      name: [{ value: '', disabled: true }, [Validators.required, Validators.minLength(2)]],
      description: [{ value: '', disabled: true }, [Validators.required]],
      monitoringType: ['km', [Validators.required]],
      kmInterval: [1, [Validators.required, Validators.min(1), Validators.max(1000000)]],
      timeIntervalWeeks: [1, [Validators.required, Validators.min(1), Validators.max(520)]],
      timeIntervalUnit: ['weeks'],
    });
  }

  ngOnInit(): void {
    const maintenance = this.maintenance();
    this.followForm.patchValue({
      name: maintenance.name,
      description: maintenance.description,
      monitoringType: (maintenance.kmInterval ?? 0) > 0 ? 'km' : 'time',
      kmInterval: maintenance.kmInterval || 1,
      timeIntervalWeeks: maintenance.timeIntervalWeeks || 1
    });
  }

  onConfirm(): void {
    if (this.followForm.invalid) {
      this.followForm.markAllAsTouched();
      this.swal.warning('Formulario incompleto', 'Por favor completa todos los campos requeridos.');
      return;
    }

    this.isFollowing.set(true);

    const followData = this.followForm.value;
    const monitoringType = this.followForm.get('monitoringType')?.value;

    const payload: FollowMaintenancePayload = {
      motorcycleId: this.isDefault() ? this.motorcycleIdContext() : this.maintenance().motorcycleId,
      defaultId: this.maintenance().id,
      trackingType: monitoringType === 'km' ? 'Km' : 'Time',
      kmInterval: monitoringType === 'km' ? followData.kmInterval : 0,
      timeIntervalWeeks: monitoringType === 'time'
        ? this.convertToWeeks(followData.timeIntervalWeeks, followData.timeIntervalUnit)
        : 0
    };

    if (!payload.motorcycleId) {
      this.isFollowing.set(false);
      this.swal.error('Error', 'Debes seleccionar una motocicleta para asociar el mantenimiento.');
      return;
    }

    this.maintenanceService.followDefaultMaintenance(payload).subscribe({
      next: () => {
        this.isFollowing.set(false);
        this.swal
          .success('Agregado!', 'El mantenimiento fue agregado a tus mantenimientos.')
          .then(() => this.saved.emit());
      },
      error: (err) => {
        this.isFollowing.set(false);
        this.swal.error('Error', this.httpError.message(err, 'No se pudo agregar el mantenimiento.'));
      },
    });
  }

  onClose(): void {
    this.closed.emit();
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
}
