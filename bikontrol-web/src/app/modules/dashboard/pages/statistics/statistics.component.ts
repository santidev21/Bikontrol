import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, effect, inject } from '@angular/core';
import { RouterModule } from '@angular/router';
import { StatisticsService } from '../../service/statistics.service';
import { SwalService } from '../../../../shared/services/swal.service';
import { HttpErrorService } from '../../../../shared/services/http-error.service';
import { AuthService } from '../../../auth/services/auth.service';

const HEALTH_COLORS: Record<string, string> = {
  'Vencido': '#ef4444',
  'Crítico': '#f97316',
  'Próximo': '#eab308',
  'OK': '#10b981'
};

const MONTH_LABELS = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];

@Component({
    selector: 'app-statistics',
    imports: [CommonModule, RouterModule],
    templateUrl: './statistics.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrl: './statistics.component.scss'
})
export class StatisticsComponent {
  private readonly statisticsService = inject(StatisticsService);
  private readonly swal = inject(SwalService);
  private readonly httpError = inject(HttpErrorService);
  private readonly authService = inject(AuthService);

  readonly summary = this.statisticsService.getSummaryResource();

  /** Safe view of the value: `value()` throws while the resource is in an error state. */
  readonly summaryData = computed(() =>
    this.summary.hasValue() ? this.summary.value() : undefined);

  readonly maxKm = computed(() =>
    Math.max(0, ...(this.summaryData()?.kmByMotorcycle.map((m) => m.km) ?? [])));

  readonly maxTypeCount = computed(() =>
    Math.max(0, ...(this.summaryData()?.recordsByType.map((t) => t.count) ?? [])));

  readonly maxMonthCount = computed(() =>
    Math.max(0, ...(this.summaryData()?.last6Months.map((m) => m.count) ?? [])));

  readonly maxHealthCount = computed(() =>
    Math.max(0, ...(this.summaryData()?.health.map((h) => h.count) ?? [])));

  constructor() {
    effect(() => {
      const error = this.summary.error();
      if (error) {
        this.swal.error('Error', this.httpError.message(error, 'No se pudieron cargar las estadísticas.'));
      }
    });
  }

  get isDemo(): boolean {
    return this.authService.isDemo();
  }

  barWidth(value: number, max: number): number {
    if (max <= 0 || value <= 0) return 0;
    return Math.round((value / max) * 100);
  }

  healthColor(bucket: string): string {
    return HEALTH_COLORS[bucket] ?? '#6b7280';
  }

  monthLabel(yearMonth: string): string {
    const parts = yearMonth.split('-');
    const month = Number(parts[1]);
    if (!month || month < 1 || month > 12) return yearMonth;
    return MONTH_LABELS[month - 1];
  }
}
