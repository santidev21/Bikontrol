import { Component } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MaintenanceService } from '../../../service/maintenance.service';
import { ActivatedRoute, Router } from '@angular/router';
import { SwalService } from '../../../../../shared/services/swal.service';
import { CommonModule } from '@angular/common';
import { SaveMaintenanceDTO } from '../../../interfaces/maintenance.interface';

@Component({
  selector: 'app-save-maintenance',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule],
  templateUrl: './save-maintenance.component.html',
  styleUrl: './save-maintenance.component.scss'
})
export class SaveMaintenanceComponent {
  maintenanceForm: FormGroup;
  isSubmitting = false;
  isEditMode = false;
  maintenanceId?: string;
  currentYear = new Date().getFullYear();

  constructor(
    private fb: FormBuilder,
    private maintenanceService: MaintenanceService,
    private router: Router,
    private route: ActivatedRoute,
    private swal: SwalService
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
    this.route.paramMap.subscribe((params) => {
      const id = params.get('id');
      if (id) {
        this.isEditMode = true;
        this.maintenanceId = id;
        this.loadMaintenanceId(this.maintenanceId);
      }
    });
  }

  loadMaintenanceId(id: string): void {
    this.maintenanceService.getById(id).subscribe({
      next: (maintenance) => {
        const mappedMaintenance = {
          ...maintenance,
          monitoringType: maintenance.TrackingType === 'Km' ? 'km' : 'time'
        };
        this.maintenanceForm.patchValue(mappedMaintenance);
        this.maintenanceForm.get('monitoringType')?.disable();
      },
      error: () => {
        this.swal.error('Error', 'No se pudo cargar la motocicleta.');
      },
    });
  }

  onSubmit(): void {
    if (this.maintenanceForm.invalid) {
      this.maintenanceForm.markAllAsTouched();
      this.swal.warning(
        'Formulario incompleto',
        'Por favor completa todos los campos requeridos.'
      );
      return;
    }

    this.isSubmitting = true;
    const maintenance: SaveMaintenanceDTO = this.maintenanceForm.value;
    const monitoringType = this.maintenanceForm.get('monitoringType')?.value;

    maintenance.kmInterval = 0;
    maintenance.timeIntervalWeeks = 0;
    maintenance.TrackingType = monitoringType === 'km' ? 'Km' : 'Time';

    if (monitoringType === 'km') {
      maintenance.kmInterval = this.maintenanceForm.get('kmInterval')?.value;
    } else {
      const timeValue = this.maintenanceForm.get('timeIntervalWeeks')?.value;
      const timeUnit = this.maintenanceForm.get('timeIntervalUnit')?.value;
      if (timeValue && timeUnit) {
        maintenance.timeIntervalWeeks = this.convertToWeeks(timeValue, timeUnit);
      }
    }
    
    if (this.isEditMode && this.maintenanceId) this.updateMaintenance(maintenance);
    else this.addMaintenance(maintenance);
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
    this.maintenanceService.createUserMaintenance(maintenance).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.swal
          .success('¡Éxito!', 'Mantenimiento agregado correctamente.')
          .then(() => this.router.navigate(['/dashboard/maintenance']));
      },
      error: (err) => {
        this.isSubmitting = false;
        this.swal.error(
          'Error',
          err?.error?.message || 'No se pudo agregar el mantenimiento.'
        );
      },
    });
  }

  updateMaintenance(maintenance: SaveMaintenanceDTO): void {
    this.maintenanceService.updateMaintenance(this.maintenanceId ?? '', maintenance).subscribe({
        next: () => {
          this.isSubmitting = false;
          this.swal
            .success('¡Éxito!', 'Mantenimiento actualizado correctamente.')
            .then(() => this.router.navigate(['/dashboard/maintenance']));
        },
        error: (err) => {
          this.isSubmitting = false;
          this.swal.error(
            'Error',
            err?.error?.message || 'No se pudo actualizar el mantenimiento.'
          );
        },
      });
  }

  hasError(field: string, type: string): boolean {
    const control = this.maintenanceForm.get(field);
    const monitoringType = this.maintenanceForm.get('monitoringType')?.value;
    
    // Only validate kmInterval if monitoringType is 'km'
    if (field === 'kmInterval' && monitoringType !== 'km') {
      return false;
    }
    
    // Only validate time-related fields if monitoringType is 'time'
    if ((field === 'timeIntervalWeeks' || field === 'timeIntervalUnit') && monitoringType !== 'time') {
      return false;
    }
    
    return !!control && control.hasError(type) && control.touched;
  }
}
