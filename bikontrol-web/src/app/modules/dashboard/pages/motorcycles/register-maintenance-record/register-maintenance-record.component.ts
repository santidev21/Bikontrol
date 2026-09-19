import { Component, OnDestroy, OnInit, ChangeDetectionStrategy, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { Maintenance, CreateMaintenanceRecordRequest } from '../../../interfaces/maintenance.interface';
import { MaintenanceService } from '../../../service/maintenance.service';
import { MotorcyclesService } from '../../../service/motorcycles.service';
import { SwalService } from '../../../../../shared/services/swal.service';
import { HttpErrorService } from '../../../../../shared/services/http-error.service';

@Component({
    selector: 'app-register-maintenance-record',
    imports: [ReactiveFormsModule],
    templateUrl: './register-maintenance-record.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrl: './register-maintenance-record.component.scss'
})
export class RegisterMaintenanceRecordComponent implements OnInit, OnDestroy {
  readonly motorcycleId = signal('');
  readonly maintenances = signal<Maintenance[]>([]);
  readonly selectedMaintenance = signal<Maintenance | undefined>(undefined);
  readonly currentKm = signal(0);
  readonly lastMaintenanceKm = signal<number | null>(null);
  readonly isSubmitting = signal(false);

  form: FormGroup;

  private readonly subscriptions = new Subscription();

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private maintenanceService: MaintenanceService,
    private motorcyclesService: MotorcyclesService,
    private swal: SwalService,
    private httpError: HttpErrorService
  ) {
    this.form = this.fb.group({
      userMaintenanceId: ['', Validators.required],
      performedAt: [this.getTodayDate(), Validators.required],
      performedKm: [null]
    });
  }

  ngOnInit(): void {
    this.subscriptions.add(
      this.route.paramMap.subscribe((params) => {
        const motorcycleId = params.get('motorcycleId');
        if (!motorcycleId) {
          this.router.navigate(['/dashboard/home']);
          return;
        }

        this.motorcycleId.set(motorcycleId);
        this.loadData();
      })
    );

    this.subscriptions.add(
      this.form.get('userMaintenanceId')?.valueChanges.subscribe((maintenanceId: string) => {
        this.selectedMaintenance.set(this.maintenances().find((m) => m.id === maintenanceId));
        this.updateKmControlByTrackingType();
        this.loadLastMaintenanceKm();
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  private loadData(): void {
    const id = this.motorcycleId();

    this.motorcyclesService.getCurrentKm(id).subscribe({
      next: (res) => {
        this.currentKm.set(res.km);
      }
    });

    this.maintenanceService.getUserMaintenanceByMotorcycle(id).subscribe({
      next: (list) => {
        this.maintenances.set(list);
      },
      error: () => {
        this.swal.error('Error', 'No se pudo cargar los mantenimientos de la moto.');
      }
    });
  }

  private updateKmControlByTrackingType(): void {
    const control = this.form.get('performedKm');
    if (!control) return;

    if (this.selectedMaintenance()?.trackingType === 'Km') {
      control.setValidators([Validators.required, Validators.min(1)]);
      control.setValue(this.currentKm());
    } else {
      control.clearValidators();
      control.setValue(null);
    }

    control.updateValueAndValidity();
  }

  private loadLastMaintenanceKm(): void {
    this.lastMaintenanceKm.set(null);
    const maintenanceId = this.form.get('userMaintenanceId')?.value;
    if (!maintenanceId) return;

    this.maintenanceService.getMaintenanceRecordsByMotorcycle(this.motorcycleId()).subscribe({
      next: (records) => {
        const last = records.find((x) => x.userMaintenanceId === maintenanceId);
        this.lastMaintenanceKm.set(last?.performedKm ?? null);
      }
    });
  }

  onSubmit(): void {
    const selected = this.selectedMaintenance();
    if (this.form.invalid || !selected) {
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
    const lastKm = this.lastMaintenanceKm();
    if (
      selected.trackingType === 'Km' &&
      lastKm != null &&
      performedKm != null &&
      performedKm < lastKm
    ) {
      this.swal.warning('Error', 'No puedes agregar mantenimiento anterior al ultimo');
      return;
    }

    const payload: CreateMaintenanceRecordRequest = {
      motorcycleId: this.motorcycleId(),
      userMaintenanceId: this.form.get('userMaintenanceId')?.value,
      performedAt: performedAt.toISOString(),
      performedKm: selected.trackingType === 'Km' ? performedKm : null
    };

    this.isSubmitting.set(true);
    this.maintenanceService.registerMaintenanceRecord(payload).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.swal.success('¡Éxito!', 'Se registró el mantenimiento correctamente.').then(() => {
          this.router.navigate(['/dashboard/motorcycles/summary'], {
            queryParams: { motorcycleId: this.motorcycleId() }
          });
        });
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.swal.error('Error', this.httpError.message(err, 'No se pudo registrar el mantenimiento.'));
      }
    });
  }

  private getTodayDate(): string {
    return new Date().toISOString().split('T')[0];
  }
}
