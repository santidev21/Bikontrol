import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Observable, forkJoin, of } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import { Maintenance } from '../../interfaces/maintenance.interface';
import { Motorcycle, SaveMotorcycleDTO } from '../../interfaces/motorcycle.interface';
import { MaintenanceService } from '../../service/maintenance.service';
import { MotorcyclesService } from '../../service/motorcycles.service';
import { AuthService } from '../../../auth/services/auth.service';
import { SwalService } from '../../../../shared/services/swal.service';
import { HttpErrorService } from '../../../../shared/services/http-error.service';
import { hasError as formHasError } from '../../../../shared/utils/form.utils';
import { IntervalFormatPipe } from '../../pipes/interval-format.pipe';

// Sensible starter set, pre-selected so a new user can finish in one tap. Names
// mirror the seeded predefined maintenance types; unknown names are simply not
// pre-selected (no crash if the catalog changes).
const RECOMMENDED_DEFAULTS = [
  'Cambio de Aceite',
  'Cambio de Filtro de Aceite',
  'Lubricación y Limpieza de Cadena',
  'Presión de Llantas',
  'Revisión General',
];

/**
 * Guided setup for a brand-new user: add the first motorcycle and pick a
 * maintenance plan in one flow, then land on the summary with both created.
 * Reuses the existing motorcycle/follow endpoints (no batch API needed).
 */
@Component({
  selector: 'app-onboarding',
  imports: [ReactiveFormsModule, IntervalFormatPipe],
  templateUrl: './onboarding.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './onboarding.component.scss',
})
export class OnboardingComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly motorcyclesService = inject(MotorcyclesService);
  private readonly maintenanceService = inject(MaintenanceService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly swal = inject(SwalService);
  private readonly httpError = inject(HttpErrorService);

  readonly step = signal<1 | 2>(1);
  readonly isSubmitting = signal(false);
  readonly loadingPlan = signal(true);
  readonly defaults = signal<Maintenance[]>([]);
  readonly selected = signal<Set<string>>(new Set());
  readonly currentYear = new Date().getFullYear();

  motorcycleForm: FormGroup;

  constructor() {
    this.motorcycleForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      brand: ['', [Validators.required]],
      year: [
        null,
        [Validators.required, Validators.min(1950), Validators.max(this.currentYear + 1)],
      ],
      nickname: ['', [Validators.required]],
      km: [0, [Validators.required, Validators.min(0), Validators.max(1000000)]],
      displacement: [null, [Validators.required, Validators.min(1), Validators.max(2300)]],
      plate: ['', [Validators.required]],
    });
  }

  ngOnInit(): void {
    // Demo accounts are read-only, so onboarding makes no sense for them.
    if (this.authService.isDemo()) {
      this.router.navigate(['/dashboard/home']);
      return;
    }
    this.loadPlan();
  }

  get selectedCount(): number {
    return this.selected().size;
  }

  private loadPlan(): void {
    this.loadingPlan.set(true);
    this.maintenanceService.getDefaultMaintenance().subscribe({
      next: (defaults) => {
        this.defaults.set(defaults);
        this.selectRecommended();
        this.loadingPlan.set(false);
      },
      error: (err) => {
        this.loadingPlan.set(false);
        this.swal.error(
          'Error',
          this.httpError.message(err, 'No se pudo cargar el plan de mantenimiento.'),
        );
      },
    });
  }

  goToPlan(): void {
    if (this.motorcycleForm.invalid) {
      this.motorcycleForm.markAllAsTouched();
      this.swal.warning('Falta un dato', 'Completa los datos de tu motocicleta para continuar.');
      return;
    }
    this.step.set(2);
  }

  back(): void {
    this.step.set(1);
  }

  isSelected(id: string): boolean {
    return this.selected().has(id);
  }

  toggle(id: string): void {
    this.selected.update((set) => {
      const next = new Set(set);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  selectRecommended(): void {
    this.selected.set(
      new Set(
        this.defaults()
          .filter((d) => RECOMMENDED_DEFAULTS.includes(d.name))
          .map((d) => d.id),
      ),
    );
  }

  clearSelection(): void {
    this.selected.set(new Set());
  }

  finish(): void {
    if (this.isSubmitting()) return;
    this.isSubmitting.set(true);

    const dto: SaveMotorcycleDTO = { ...this.motorcycleForm.value, image: 'default.png' };
    const chosen = this.defaults().filter((d) => this.selected().has(d.id));

    this.motorcyclesService
      .addMotorcycle(dto)
      .pipe(
        switchMap((motorcycle) =>
          this.followPlan(motorcycle.id!, chosen).pipe(
            map((failures) => ({ motorcycle, failures })),
          ),
        ),
      )
      .subscribe({
        next: ({ motorcycle, failures }) => {
          this.isSubmitting.set(false);
          const done = () => this.goToSummary(motorcycle);
          if (failures > 0) {
            // The motorcycle is already created; surface the partial failure
            // instead of hiding it, and let the user finish from the catalog.
            this.swal
              .warning(
                'Casi listo',
                `Tu moto quedó creada, pero ${failures} mantenimiento(s) no se pudieron agregar. Podés sumarlos desde el catálogo.`,
              )
              .then(done);
          } else {
            this.swal
              .success('¡Todo listo!', 'Tu motocicleta y plan de mantenimiento están configurados.')
              .then(done);
          }
        },
        error: (err) => {
          this.isSubmitting.set(false);
          this.swal.error(
            'Error',
            this.httpError.message(err, 'No se pudo completar la configuración.'),
          );
        },
      });
  }

  /** Follows each chosen default; a single failure does not abort the rest. */
  private followPlan(motorcycleId: string, chosen: Maintenance[]): Observable<number> {
    if (chosen.length === 0) {
      return of(0);
    }

    const calls = chosen.map((maintenance) =>
      this.maintenanceService
        .followDefaultMaintenance({
          motorcycleId,
          defaultId: maintenance.id,
          trackingType: maintenance.trackingType,
          kmInterval: maintenance.trackingType === 'Km' ? (maintenance.kmInterval ?? 1) : 0,
          timeIntervalWeeks:
            maintenance.trackingType === 'Time' ? (maintenance.timeIntervalWeeks ?? 1) : 0,
        })
        .pipe(catchError(() => of(null))),
    );

    return forkJoin(calls).pipe(map((results) => results.filter((r) => r === null).length));
  }

  private goToSummary(motorcycle: Motorcycle): void {
    this.router.navigate(['/dashboard/motorcycles/summary'], { state: { motorcycle } });
  }

  hasError(field: string, type: string): boolean {
    return formHasError(this.motorcycleForm, field, type);
  }
}
