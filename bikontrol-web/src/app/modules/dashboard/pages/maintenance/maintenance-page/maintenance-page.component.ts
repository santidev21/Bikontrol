import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MaintenanceService } from '../../../service/maintenance.service';
import { MaintenanceInfoCardComponent } from '../../../components/maintenance-info-card/maintenance-info-card.component';
import { SwalService } from '../../../../../shared/services/swal.service';
import { HttpErrorService } from '../../../../../shared/services/http-error.service';
import { AuthService } from '../../../../auth/services/auth.service';

import { TranslatePipe } from '../../../../../shared/i18n/translate.pipe';
import { I18nService } from '../../../../../shared/i18n/i18n.service';

@Component({
  selector: 'app-maintenance-page',
  imports: [RouterModule, MaintenanceInfoCardComponent, TranslatePipe],
  templateUrl: './maintenance-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './maintenance-page.component.scss',
})
export class MaintenancePageComponent implements OnInit {
  private readonly maintenanceService = inject(MaintenanceService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly swal = inject(SwalService);
  private readonly httpError = inject(HttpErrorService);
  private readonly authService = inject(AuthService);
  private readonly i18n = inject(I18nService);

  readonly motorcycleId = signal('');
  readonly isDemo = computed(() => this.authService.isDemo());

  private readonly userMaintenanceId = computed(() => this.motorcycleId() || undefined);

  private readonly userRes = this.maintenanceService.getUserMaintenanceByMotorcycleResource(
    this.userMaintenanceId,
  );
  private readonly defaultsRes = this.maintenanceService.getDefaultsResource();

  readonly userMaintenance = computed(() => (this.userRes.hasValue() ? this.userRes.value()! : []));
  readonly defaultMaintenance = computed(() =>
    this.defaultsRes.hasValue() ? this.defaultsRes.value()! : [],
  );

  constructor() {
    effect(() => {
      const error = this.userRes.error();
      if (error) {
        this.swal.error('Error', this.httpError.message(error, this.i18n.t('maint.loadError')));
      }
    });

    effect(() => {
      const error = this.defaultsRes.error();
      if (error) {
        this.swal.error(
          'Error',
          this.httpError.message(error, this.i18n.t('maint.defaultsLoadError')),
        );
      }
    });
  }

  ngOnInit(): void {
    const motorcycleId = this.route.snapshot.paramMap.get('motorcycleId');
    if (!motorcycleId) {
      this.swal.warning(
        this.i18n.t('maint.contextRequiredTitle'),
        this.i18n.t('maint.contextRequiredText'),
      );
      this.router.navigate(['/dashboard/home']);
      return;
    }

    this.motorcycleId.set(motorcycleId);
  }

  reload(): void {
    this.userRes.reload();
    this.defaultsRes.reload();
  }

  goToAddMaintenance(): void {
    // `maintenance/add` must be two path segments: a slash inside a single array
    // element is URL-encoded and the route would never match (falls back to the
    // dashboard catch-all).
    this.router.navigate(['/dashboard/motorcycles', this.motorcycleId(), 'maintenance', 'add']);
  }
}
