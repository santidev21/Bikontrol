import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import Swal from 'sweetalert2';
import { SaveMotorcycleDTO , Motorcycle } from '../../../interfaces/motorcycle.interface';
import { CommonModule } from '@angular/common';
import { MotorcyclesService } from '../../../service/motorcycles.service';
import { SwalService } from '../../../../../shared/services/swal.service';


@Component({
  selector: 'app-save-motorcycle',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule],
  templateUrl: './save-motorcycle.component.html',
  styleUrl: './save-motorcycle.component.scss'
})
export class SaveMotorcycleComponent implements OnInit {
  motorcycleForm: FormGroup;
  isSubmitting = false;
  isEditMode = false;
  motorcycleId?: string;
  currentYear = new Date().getFullYear();

  constructor(
    private fb: FormBuilder,
    private motorcyclesService: MotorcyclesService,
    private router: Router,
    private route: ActivatedRoute,
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
          Validators.max(this.currentYear + 1),
        ],
      ],
      nickname: ['', [Validators.required]],
      km: [
        0,
        [Validators.required, Validators.min(0), Validators.max(1000000)],
      ],
      displacement: [
        null,
        [Validators.required, Validators.min(1), Validators.max(2300)],
      ],
      plate: ['', [Validators.required]],
      image: ['default.png'],
      isEnabled: [true],
    });
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const id = params.get('id');
      if (id) {
        this.isEditMode = true;
        this.motorcycleId = id;
        this.loadMotorcycle(this.motorcycleId);
      }
    });
  }

  loadMotorcycle(id: string): void {
    this.motorcyclesService.getById(id).subscribe({
      next: (motorcycle) => {
        this.motorcycleForm.patchValue(motorcycle);
      },
      error: () => {
        this.swal.error('Error', 'No se pudo cargar la motocicleta.');
      },
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
    const motorcycle: SaveMotorcycleDTO = this.motorcycleForm.value;

    if (this.isEditMode && this.motorcycleId) this.updateMotorcycle(motorcycle);
    else this.addMotorcycle(motorcycle);
  }

  addMotorcycle(motorcycle: SaveMotorcycleDTO): void {
    this.motorcyclesService.addMotorcycle(motorcycle).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.swal
          .success('¡Éxito!', 'Motocicleta agregada correctamente.')
          .then(() => this.router.navigate(['/dashboard']));
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

  updateMotorcycle(motorcycle: SaveMotorcycleDTO): void {
    this.motorcyclesService.updateMotorcycle(this.motorcycleId ?? '', motorcycle).subscribe({
        next: () => {
          this.isSubmitting = false;
          this.swal
            .success('¡Éxito!', 'Motocicleta actualizada correctamente.')
            .then(() => this.router.navigate(['/dashboard']));
        },
        error: (err) => {
          this.isSubmitting = false;
          this.swal.error(
            'Error',
            err?.error?.message || 'No se pudo actualizar la motocicleta.'
          );
        },
      });
  }

  hasError(field: string, type: string): boolean {
    const control = this.motorcycleForm.get(field);
    return !!control && control.hasError(type) && control.touched;
  }
}
