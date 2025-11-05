import { Component } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import Swal from 'sweetalert2';
import { CreateMotorcycleDTO, Motorcycle } from '../../../interfaces/motorcycle.interface';
import { CommonModule } from '@angular/common';
import { MotorcyclesService } from '../../../service/motorcycles.service';
import { SwalService } from '../../../../../shared/services/swal.service';


@Component({
  selector: 'app-add-motorcycle',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule],
  templateUrl: './add-motorcycle.component.html',
  styleUrl: './add-motorcycle.component.scss'
})
export class AddMotorcycleComponent {
 motorcycleForm: FormGroup;
  isSubmitting = false;

  constructor(
    private fb: FormBuilder,
    private motorcyclesService: MotorcyclesService,
    private router: Router,
    private swal: SwalService
  ) {
    this.motorcycleForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      brand: ['', [Validators.required]],
      year: [
        null,
        [
          Validators.required,
          Validators.min(1950),
          Validators.max(new Date().getFullYear()),
        ],
      ],
      nickname: ['', [Validators.required]],
      km: [0, [Validators.required, Validators.min(0)]],
      displacement: [null, [Validators.required, Validators.min(50)]],
      plate: ['', [Validators.required]],
      image: ['/assets/images/defaults/motorcycle-placeholder.webp'],
      isEnabled: [true],
    });
  }

  onSubmit(): void {
    if (this.motorcycleForm.invalid) {
      this.motorcycleForm.markAllAsTouched();
      this.swal.warning(
        'Formulario incompleto',
        'Por favor completa todos los campos requeridos.'
      );
      return;
    }

    this.isSubmitting = true;
    const motorcycle: CreateMotorcycleDTO = this.motorcycleForm.value;

    this.motorcyclesService.addMotorcycle(motorcycle).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.swal.success('¡Éxito!', 'Motocicleta agregada correctamente.').then(
          () => this.router.navigate(['/dashboard'])
        );
      },
      error: (err) => {
        this.isSubmitting = false;
        this.swal.error(
          'Error',
          err?.error?.message || 'No se pudo agregar la motocicleta.'
        );
      },
    });
  }

  hasError(field: string, type: string): boolean {
    const control = this.motorcycleForm.get(field);
    return !!control && control.hasError(type) && control.touched;
  }
}
