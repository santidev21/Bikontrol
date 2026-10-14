import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { FuelUp, SaveFuelUpDTO } from '../../../interfaces/fuel-up.interface';
import { FuelUpsService } from '../../../service/fuel-ups.service';
import { StatisticsService } from '../../../service/statistics.service';
import { SwalService } from '../../../../../shared/services/swal.service';
import { HttpErrorService } from '../../../../../shared/services/http-error.service';
import { AuthService } from '../../../../auth/services/auth.service';
import { SpinnerComponent } from '../../../../../shared/components/spinner/spinner.component';
import {
  hasError as formHasError,
  isInvalid as formIsInvalid,
} from '../../../../../shared/utils/form.utils';

import { TranslatePipe } from '../../../../../shared/i18n/translate.pipe';
import { I18nService } from '../../../../../shared/i18n/i18n.service';

function todayAsInputValue(): string {
  return new Date().toISOString().split('T')[0];
}

function toDateInputValue(iso: string): string {
  return iso.slice(0, 10);
}

@Component({
  selector: 'app-fuel-page',
  imports: [CommonModule, ReactiveFormsModule, SpinnerComponent, TranslatePipe],
  templateUrl: './fuel-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './fuel-page.component.scss',
})
export class FuelPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fuelUpsService = inject(FuelUpsService);
  private readonly statisticsService = inject(StatisticsService);
  private readonly swal = inject(SwalService);
  private readonly httpError = inject(HttpErrorService);
  private readonly authService = inject(AuthService);
  private readonly i18n = inject(I18nService);

  readonly motorcycleId = signal<string | undefined>(undefined);
  readonly editingId = signal<string | null>(null);
  readonly isSubmitting = signal(false);
  readonly deletingId = signal<string | null>(null);
  readonly submitted = signal(false);

  form: FormGroup;

  private readonly listRes = this.fuelUpsService.getByMotorcycleResource(this.motorcycleId);
  private readonly consumptionRes = this.fuelUpsService.getConsumptionResource(this.motorcycleId);
  /** Best-effort read of the budget status; the banner is optional, never blocking. */
  private readonly summaryRes = this.statisticsService.getSummaryResource();

  readonly fuelUps = computed(() => (this.listRes.hasValue() ? this.listRes.value()! : []));
  readonly consumption = computed(() =>
    this.consumptionRes.hasValue() ? this.consumptionRes.value() : undefined,
  );
  readonly isListLoading = computed(() => this.listRes.isLoading());
  readonly isConsumptionLoading = computed(() => this.consumptionRes.isLoading());

  readonly isDemo = computed(() => this.authService.isDemo());
  readonly isEditing = computed(() => this.editingId() !== null);
  readonly points = computed(() => this.consumption()?.points ?? []);
  readonly hasTrend = computed(() => this.points().length > 0);
  readonly maxKmPerLitre = computed(() =>
    Math.max(0, ...this.points().map((point) => point.kmPerLitre ?? 0)),
  );
  readonly hasConsumptionDrop = computed(() => this.consumption()?.isConsumptionDrop === true);
  readonly budgetOver = computed(() => {
    const budget = this.summaryRes.hasValue() ? this.summaryRes.value()?.budget : undefined;
    return !!budget && (budget.isMonthlyOverBudget || budget.isAnnualOverBudget);
  });

  constructor() {
    this.form = this.fb.group({
      fueledAt: [todayAsInputValue(), Validators.required],
      odometerKm: [null, [Validators.required, Validators.min(0)]],
      litres: [null, [Validators.required, Validators.min(0.01)]],
      totalCost: [null, [Validators.required, Validators.min(0)]],
      isFullTank: [true],
      station: [''],
      notes: [''],
    });

    effect(() => {
      const error = this.listRes.error();
      if (error) {
        this.swal.error(
          this.i18n.t('common.error'),
          this.httpError.message(error, this.i18n.t('fuel.loadError')),
        );
      }
    });

    effect(() => {
      const error = this.consumptionRes.error();
      if (error) {
        this.swal.error(
          this.i18n.t('common.error'),
          this.httpError.message(error, this.i18n.t('fuel.consumptionLoadError')),
        );
      }
    });
  }

  ngOnInit(): void {
    const motorcycleId = this.route.snapshot.paramMap.get('motorcycleId');
    if (!motorcycleId) {
      this.swal.warning(
        this.i18n.t('fuel.contextRequiredTitle'),
        this.i18n.t('fuel.contextRequiredText'),
      );
      this.router.navigate(['/dashboard/home']);
      return;
    }

    this.motorcycleId.set(motorcycleId);
  }

  reload(): void {
    this.listRes.reload();
    this.consumptionRes.reload();
  }

  onSubmit(): void {
    if (this.isDemo()) return;

    this.submitted.set(true);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.swal.warning(
        this.i18n.t('common.formIncomplete'),
        this.i18n.t('common.completeRequired'),
      );
      return;
    }

    const motorcycleId = this.motorcycleId();
    if (!motorcycleId || this.isSubmitting()) return;

    const dto = this.buildPayload(motorcycleId);
    const editingId = this.editingId();

    this.isSubmitting.set(true);
    const request = editingId
      ? this.fuelUpsService.update(editingId, dto)
      : this.fuelUpsService.create(dto);

    request.subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.resetForm();
        this.reload();
        this.swal.success(
          this.i18n.t('common.success'),
          this.i18n.t(editingId ? 'fuel.updated' : 'fuel.added'),
        );
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.swal.error(
          this.i18n.t('common.error'),
          this.httpError.message(err, this.i18n.t('fuel.saveError')),
        );
      },
    });
  }

  startEdit(fuelUp: FuelUp): void {
    if (this.isDemo()) return;

    this.editingId.set(fuelUp.id);
    this.submitted.set(false);
    this.form.reset({
      fueledAt: toDateInputValue(fuelUp.fueledAt),
      odometerKm: fuelUp.odometerKm,
      litres: fuelUp.litres,
      totalCost: fuelUp.totalCost,
      isFullTank: fuelUp.isFullTank,
      station: fuelUp.station ?? '',
      notes: fuelUp.notes ?? '',
    });
  }

  cancelEdit(): void {
    this.resetForm();
  }

  confirmDelete(fuelUp: FuelUp): void {
    if (this.isDemo() || this.deletingId()) return;

    this.swal
      .confirm(
        this.i18n.t('fuel.deleteConfirmTitle'),
        this.i18n.t('fuel.deleteConfirmText', { date: toDateInputValue(fuelUp.fueledAt) }),
        this.i18n.t('common.yesDelete'),
        this.i18n.t('common.cancel'),
        'warning',
      )
      .then((result) => {
        if (result.isConfirmed) this.deleteFuelUp(fuelUp.id);
      });
  }

  deleteFuelUp(id: string): void {
    if (this.isDemo() || this.deletingId()) return;

    this.deletingId.set(id);
    this.fuelUpsService.delete(id).subscribe({
      next: () => {
        this.deletingId.set(null);
        if (this.editingId() === id) this.resetForm();
        this.reload();
        this.swal.success(this.i18n.t('fuel.deletedTitle'), this.i18n.t('fuel.deletedText'));
      },
      error: (err) => {
        this.deletingId.set(null);
        this.swal.error(
          this.i18n.t('common.error'),
          this.httpError.message(err, this.i18n.t('fuel.deleteError')),
        );
      },
    });
  }

  goToSummary(): void {
    const id = this.motorcycleId();
    if (!id) return;
    this.router.navigate(['/dashboard/motorcycles/summary'], {
      queryParams: { motorcycleId: id },
    });
  }

  hasError(field: string, type: string): boolean {
    return formHasError(this.form, field, type);
  }

  isInvalid(field: string): boolean {
    return formIsInvalid(this.form, field, this.submitted());
  }

  /** Bar height (%) of a consumption point relative to the best segment. */
  barWidth(value: number | null | undefined, max: number): number {
    if (value == null || max <= 0 || value <= 0) return 0;
    return Math.round((value / max) * 100);
  }

  /** Human-readable overspend sentence for the current budget status. */
  budgetOverMessage(): string {
    const budget = this.summaryRes.hasValue() ? this.summaryRes.value()?.budget : undefined;
    const periodKey = budget?.isMonthlyOverBudget ? 'fuel.budgetOverMonth' : 'fuel.budgetOverYear';
    return this.i18n.t('fuel.budgetOverText', { period: this.i18n.t(periodKey) });
  }

  private buildPayload(motorcycleId: string): SaveFuelUpDTO {
    const raw = this.form.getRawValue();
    return {
      motorcycleId,
      fueledAt: new Date(`${raw.fueledAt}T00:00:00`).toISOString(),
      odometerKm: Number(raw.odometerKm),
      litres: Number(raw.litres),
      totalCost: Number(raw.totalCost),
      isFullTank: !!raw.isFullTank,
      station: this.optionalText(raw.station),
      notes: this.optionalText(raw.notes),
    };
  }

  private optionalText(value: unknown): string | null {
    const text = typeof value === 'string' ? value.trim() : '';
    return text.length > 0 ? text : null;
  }

  private resetForm(): void {
    this.editingId.set(null);
    this.submitted.set(false);
    this.form.reset({
      fueledAt: todayAsInputValue(),
      odometerKm: null,
      litres: null,
      totalCost: null,
      isFullTank: true,
      station: '',
      notes: '',
    });
  }
}
