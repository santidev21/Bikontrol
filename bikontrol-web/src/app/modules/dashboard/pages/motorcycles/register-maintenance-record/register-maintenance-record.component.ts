import {
  Component,
  OnDestroy,
  OnInit,
  ChangeDetectionStrategy,
  computed,
  signal,
  inject,
} from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription, catchError, concatMap, from, map, of, toArray } from 'rxjs';
import {
  Maintenance,
  CreateMaintenanceRecordRequest,
} from '../../../interfaces/maintenance.interface';
import { MaintenanceService } from '../../../service/maintenance.service';
import { MotorcyclesService } from '../../../service/motorcycles.service';
import { SwalService } from '../../../../../shared/services/swal.service';
import { HttpErrorService } from '../../../../../shared/services/http-error.service';

import { TranslatePipe } from '../../../../../shared/i18n/translate.pipe';
import { I18nService } from '../../../../../shared/i18n/i18n.service';

@Component({
  selector: 'app-register-maintenance-record',
  imports: [ReactiveFormsModule, TranslatePipe],
  templateUrl: './register-maintenance-record.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './register-maintenance-record.component.scss',
})
export class RegisterMaintenanceRecordComponent implements OnInit, OnDestroy {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private maintenanceService = inject(MaintenanceService);
  private motorcyclesService = inject(MotorcyclesService);
  private swal = inject(SwalService);
  private httpError = inject(HttpErrorService);
  private i18n = inject(I18nService);

  readonly motorcycleId = signal('');
  readonly maintenances = signal<Maintenance[]>([]);
  readonly currentKm = signal(0);
  readonly isSubmitting = signal(false);
  readonly loaded = signal(false);

  /** Ids of the maintenances the user checked, in list order. */
  readonly selectedIds = signal<string[]>([]);
  /** Optional cost per maintenance id. */
  readonly costs = signal<Record<string, number | null>>({});
  /** Last performed km per maintenance id (null when it has no record yet). */
  readonly lastKmByMaintenance = signal<Record<string, number | null>>({});

  readonly selectedMaintenances = computed(() =>
    this.maintenances().filter((m) => this.selectedIds().includes(m.id)),
  );
  readonly anyKmSelected = computed(() =>
    this.selectedMaintenances().some((m) => m.trackingType === 'Km'),
  );

  form: FormGroup;

  private preselectId: string | null = null;

  private readonly subscriptions = new Subscription();

  constructor() {
    this.form = this.fb.group({
      performedAt: [this.getTodayDate(), Validators.required],
      performedKm: [null],
    });
  }

  ngOnInit(): void {
    this.preselectId = this.route.snapshot.queryParamMap.get('userMaintenanceId');

    this.subscriptions.add(
      this.route.paramMap.subscribe((params) => {
        const motorcycleId = params.get('motorcycleId');
        if (!motorcycleId) {
          this.router.navigate(['/dashboard/home']);
          return;
        }

        this.motorcycleId.set(motorcycleId);
        this.loadData();
      }),
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  isSelected(id: string): boolean {
    return this.selectedIds().includes(id);
  }

  toggleSelection(id: string): void {
    const nowSelected = !this.isSelected(id);
    this.selectedIds.update((ids) =>
      nowSelected ? [...ids, id] : ids.filter((current) => current !== id),
    );

    if (!nowSelected) {
      this.costs.update((costs) => {
        const { [id]: _removed, ...rest } = costs;
        return rest;
      });
    }

    this.updateKmValidators();
  }

  costOf(id: string): number | null {
    return this.costs()[id] ?? null;
  }

  setCost(id: string, event: Event): void {
    const raw = (event.target as HTMLInputElement).value;
    const value = raw === '' ? null : Number(raw);
    this.costs.update((costs) => ({ ...costs, [id]: value }));
  }

  private loadData(): void {
    const id = this.motorcycleId();

    this.motorcyclesService.getCurrentKm(id).subscribe({
      next: (res) => {
        this.currentKm.set(res.km);
        // Default the shared km field once the odometer is known.
        const kmControl = this.form.get('performedKm');
        if (this.anyKmSelected() && (kmControl?.value === null || kmControl?.value === 0)) {
          kmControl.setValue(res.km);
        }
      },
    });

    this.maintenanceService.getUserMaintenanceByMotorcycle(id).subscribe({
      next: (list) => {
        this.maintenances.set(list);
        this.loaded.set(true);
        this.applyPreselection();
      },
      error: () => {
        this.loaded.set(true);
        this.swal.error(this.i18n.t('common.error'), this.i18n.t('record.loadError'));
      },
    });

    this.maintenanceService.getMaintenanceRecordsByMotorcycle(id).subscribe({
      next: (records) => {
        const map: Record<string, number | null> = {};
        for (const record of records) {
          if (!(record.userMaintenanceId in map)) {
            map[record.userMaintenanceId] = record.performedKm ?? null;
          }
        }
        this.lastKmByMaintenance.set(map);
      },
    });
  }

  /** Selects the maintenance passed through `?userMaintenanceId=` (deep link). */
  private applyPreselection(): void {
    if (!this.preselectId) return;
    if (!this.maintenances().some((m) => m.id === this.preselectId)) return;

    this.selectedIds.set([this.preselectId]);
    this.updateKmValidators();
  }

  private updateKmValidators(): void {
    const control = this.form.get('performedKm');
    if (!control) return;

    if (this.anyKmSelected()) {
      control.setValidators([Validators.required, Validators.min(1)]);
      if (control.value === null || control.value === 0) {
        control.setValue(this.currentKm());
      }
    } else {
      control.clearValidators();
      control.setValue(null);
    }

    control.updateValueAndValidity();
  }

  onSubmit(): void {
    const selected = this.selectedMaintenances();
    if (selected.length === 0) {
      this.swal.warning(this.i18n.t('common.error'), this.i18n.t('record.selectAtLeastOne'));
      return;
    }

    const performedKm = this.form.get('performedKm')?.value as number | null;
    if (this.anyKmSelected() && (performedKm == null || performedKm < 1)) {
      this.swal.warning(this.i18n.t('common.error'), this.i18n.t('record.kmRequired'));
      return;
    }

    const performedAt = new Date(this.form.get('performedAt')?.value as string);
    if (performedAt > new Date(this.getTodayDate())) {
      this.swal.warning(this.i18n.t('common.error'), this.i18n.t('record.futureDate'));
      return;
    }

    const lastKmMap = this.lastKmByMaintenance();
    const tooEarly = selected.find(
      (m) =>
        m.trackingType === 'Km' &&
        performedKm != null &&
        lastKmMap[m.id] != null &&
        performedKm < (lastKmMap[m.id] as number),
    );
    if (tooEarly) {
      this.swal.warning(this.i18n.t('common.error'), this.i18n.t('record.beforeLast'));
      return;
    }

    this.isSubmitting.set(true);

    from(selected)
      .pipe(
        concatMap((maintenance) =>
          this.maintenanceService.registerMaintenanceRecord(this.buildPayload(maintenance)).pipe(
            map(() => ({ name: maintenance.name, ok: true as const })),
            catchError((error) => of({ name: maintenance.name, ok: false as const, error })),
          ),
        ),
        toArray(),
      )
      .subscribe((results) => {
        this.isSubmitting.set(false);

        const failed = results.filter((result) => !result.ok);
        if (failed.length === 0) {
          this.swal
            .success(this.i18n.t('common.success'), this.i18n.t('record.saved'))
            .then(() => this.goToSummary());
          return;
        }

        if (failed.length === results.length) {
          this.swal.error(
            this.i18n.t('common.error'),
            this.httpError.message(failed[0].error, this.i18n.t('record.saveError')),
          );
          return;
        }

        this.swal
          .warning(
            this.i18n.t('record.partialTitle'),
            this.i18n.t('record.partialText', { names: failed.map((f) => f.name).join(', ') }),
          )
          .then(() => this.goToSummary());
      });
  }

  private buildPayload(maintenance: Maintenance): CreateMaintenanceRecordRequest {
    const performedKm = this.form.get('performedKm')?.value as number | null;
    return {
      motorcycleId: this.motorcycleId(),
      userMaintenanceId: maintenance.id,
      performedAt: new Date(this.form.get('performedAt')?.value as string).toISOString(),
      performedKm: maintenance.trackingType === 'Km' ? performedKm : null,
      cost: this.costs()[maintenance.id] ?? null,
    };
  }

  private goToSummary(): void {
    this.router.navigate(['/dashboard/motorcycles/summary'], {
      queryParams: { motorcycleId: this.motorcycleId() },
    });
  }

  private getTodayDate(): string {
    return new Date().toISOString().split('T')[0];
  }
}
