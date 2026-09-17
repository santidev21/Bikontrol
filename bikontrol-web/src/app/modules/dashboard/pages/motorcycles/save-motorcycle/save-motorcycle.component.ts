import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import Swal from 'sweetalert2';
import { SaveMotorcycleDTO , Motorcycle } from '../../../interfaces/motorcycle.interface';
import { CommonModule } from '@angular/common';
import { MotorcyclesService } from '../../../service/motorcycles.service';
import { SwalService } from '../../../../../shared/services/swal.service';
import { HttpErrorService } from '../../../../../shared/services/http-error.service';


@Component({
  selector: 'app-save-motorcycle',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule],
  templateUrl: './save-motorcycle.component.html',
  styleUrl: './save-motorcycle.component.scss'
})
export class SaveMotorcycleComponent implements OnInit, OnDestroy {
  motorcycleForm: FormGroup;
  isSubmitting = false;
  isEditMode = false;
  motorcycleId?: string;
  currentYear = new Date().getFullYear();

  private readonly subscriptions = new Subscription();

  constructor(
    private fb: FormBuilder,
    private motorcyclesService: MotorcyclesService,
    private router: Router,
    private route: ActivatedRoute,
    private swal: SwalService,
    private httpError: HttpErrorService
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
    this.subscriptions.add(
      this.route.paramMap.subscribe((params) => {
        const id = params.get('id');
        if (id) {
          this.isEditMode = true;
          this.motorcycleId = id;
          this.loadMotorcycle(this.motorcycleId);
        }
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  loadMotorcycle(id: string): void {
    this.motorcyclesService.getById(id).subscribe({
      next: (motorcycle) => {
        this.motorcycleForm.patchValue(motorcycle);
        if (this.isEditMode) {
          this.loadCurrentKm();
        }
      },
      error: (err) => {
        this.swal.error('Error', this.httpError.message(err, 'No se pudo cargar la motocicleta.'));
      },
    });
  }

  loadCurrentKm(): void {
    if (!this.motorcycleId) {
      return;
    }
    this.motorcyclesService.getCurrentKm(this.motorcycleId).subscribe({
      next: (res) => {
        this.motorcycleForm.patchValue({ km: res.km });
        this.motorcycleForm.get('km')?.disable();
      },
      error: () => {
        this.motorcycleForm.get('km')?.disable();
      },
    });
  }

  get previewSrc(): string {
    const image = this.motorcycleForm.get('image')?.value;
    return image && image !== 'default.png'
      ? image
      : '/assets/images/defaults/motorcycle-placeholder.webp';
  }

  onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }
    if (!file.type.startsWith('image/')) {
      this.swal.warning('Archivo inválido', 'Selecciona un archivo de imagen válido.');
      input.value = '';
      return;
    }
    if (file.size > 2 * 1024 * 1024) {
      this.swal.warning('Archivo muy grande', 'La imagen no puede superar 2 MB.');
      input.value = '';
      return;
    }
    const reader = new FileReader();
    reader.onload = () => {
      this.resizeImage(reader.result as string, input);
    };
    reader.readAsDataURL(file);
  }

  removeImage(): void {
    this.motorcycleForm.patchValue({ image: 'default.png' });
  }

  private resizeImage(dataUrl: string, input: HTMLInputElement): void {
    const img = new Image();
    img.onload = () => {
      const maxDim = 400;
      const scale = Math.min(1, maxDim / Math.max(img.width, img.height));
      const canvas = document.createElement('canvas');
      canvas.width = Math.round(img.width * scale);
      canvas.height = Math.round(img.height * scale);
      canvas.getContext('2d')?.drawImage(img, 0, 0, canvas.width, canvas.height);
      this.motorcycleForm.patchValue({ image: canvas.toDataURL('image/jpeg', 0.8) });
    };
    img.onerror = () => {
      this.swal.warning('Archivo inválido', 'No se pudo leer la imagen seleccionada.');
      input.value = '';
    };
    img.src = dataUrl;
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
          this.httpError.message(err, 'No se pudo agregar la motocicleta.')
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
            this.httpError.message(err, 'No se pudo actualizar la motocicleta.')
          );
        },
      });
  }

  hasError(field: string, type: string): boolean {
    const control = this.motorcycleForm.get(field);
    return !!control && control.hasError(type) && control.touched;
  }
}
