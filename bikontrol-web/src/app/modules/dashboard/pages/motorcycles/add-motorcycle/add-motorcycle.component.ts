import { Component } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import Swal from 'sweetalert2';
import { Motorcycle } from '../../../interfaces/motorcycle.interface';
import { CommonModule } from '@angular/common';

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
    private fb: FormBuilder
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
      Swal.fire('Formulario incompleto', 'Por favor completa todos los campos requeridos.', 'warning');
      return;
    }

    this.isSubmitting = true;
    const motorcycle: Motorcycle = this.motorcycleForm.value;

    // this.motorcyclesService.addMotorcycle(motorcycle).subscribe({
    //   next: () => {
    //     this.isSubmitting = false;
    //     Swal.fire('¡Éxito!', 'Motocicleta agregada correctamente.', 'success');
    //     this.motorcycleForm.reset({
    //       image: '/assets/images/defaults/motorcycle-placeholder.webp',
    //       isEnabled: true,
    //     });
    //   },
    //   error: (err) => {
    //     this.isSubmitting = false;
    //     console.error(err);
    //     Swal.fire('Error', 'No se pudo agregar la motocicleta.', 'error');
    //   },
    // });
  }

  hasError(field: string, type: string): boolean {
    const control = this.motorcycleForm.get(field);
    return !!control && control.hasError(type) && control.touched;
  }
}
