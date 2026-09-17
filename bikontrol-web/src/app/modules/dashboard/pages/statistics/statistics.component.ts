import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';
import { StatisticsSummary } from '../../interfaces/statistics.interface';
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
    styleUrl: './statistics.component.scss'
})
export class StatisticsComponent implements OnInit {
  summary?: StatisticsSummary;
  isLoading = true;

  constructor(
    private statisticsService: StatisticsService,
    private swal: SwalService,
    private httpError: HttpErrorService,
    private authService: AuthService
  ) {}

  get isDemo(): boolean {
    return this.authService.isDemo();
  }

  ngOnInit(): void {
    this.loadSummary();
  }

  loadSummary(): void {
    this.isLoading = true;
    this.statisticsService.getSummary().subscribe({
      next: (data) => {
        this.summary = data;
        this.isLoading = false;
      },
      error: (err) => {
        this.isLoading = false;
        this.swal.error('Error', this.httpError.message(err, 'No se pudieron cargar las estadísticas.'));
      }
    });
  }

  maxKm(): number {
    return Math.max(0, ...(this.summary?.kmByMotorcycle.map((m) => m.km) ?? []));
  }

  maxTypeCount(): number {
    return Math.max(0, ...(this.summary?.recordsByType.map((t) => t.count) ?? []));
  }

  maxMonthCount(): number {
    return Math.max(0, ...(this.summary?.last6Months.map((m) => m.count) ?? []));
  }

  maxHealthCount(): number {
    return Math.max(0, ...(this.summary?.health.map((h) => h.count) ?? []));
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
