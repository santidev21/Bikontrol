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
      kmInterval: [
        0,
        [Validators.required, Validators.min(0), Validators.max(1000000)],
      ],
      timeIntervalWeeks: [
        0,
        [Validators.required, Validators.min(0), Validators.max(520)],
      ],
      timeIntervalUnit: ['weeks', [Validators.required]],
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
    // this.motorcyclesService.getById(id).subscribe({
    //   next: (motorcycle) => {
    //     this.motorcycleForm.patchValue(motorcycle);
    //   },
    //   error: () => {
    //     this.swal.error('Error', 'No se pudo cargar la motocicleta.');
    //   },
    // });
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

    if (this.isEditMode && this.maintenanceId) this.updateMaintenance(maintenance);
    else this.addMaintenance(maintenance);
  }

  addMaintenance(maintenance: SaveMaintenanceDTO): void {
    this.maintenanceService.createUserMaintenance(maintenance).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.swal
          .success('¡Éxito!', 'Mantenimiento agregadd correctamente.')
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
    // this.motorcyclesService.updateMotorcycle(this.motorcycleId ?? '', motorcycle).subscribe({
    //     next: () => {
    //       this.isSubmitting = false;
    //       this.swal
    //         .success('¡Éxito!', 'Motocicleta actualizada correctamente.')
    //         .then(() => this.router.navigate(['/dashboard']));
    //     },
    //     error: (err) => {
    //       this.isSubmitting = false;
    //       this.swal.error(
    //         'Error',
    //         err?.error?.message || 'No se pudo actualizar la motocicleta.'
    //       );
    //     },
    //   });
  }

  hasError(field: string, type: string): boolean {
    const control = this.maintenanceForm.get(field);
    return !!control && control.hasError(type) && control.touched;
  }
}
