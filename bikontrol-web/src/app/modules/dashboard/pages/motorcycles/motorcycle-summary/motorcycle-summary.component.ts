import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { Motorcycle } from '../../../interfaces/motorcycle.interface';
import {
  MaintenanceRecord,
  UpcomingMaintenance,
  MaintenanceAttachment,
} from '../../../interfaces/maintenance.interface';
import { MaintenanceService } from '../../../service/maintenance.service';
import { MotorcyclesService } from '../../../service/motorcycles.service';
import { SwalService } from '../../../../../shared/services/swal.service';
import { HttpErrorService } from '../../../../../shared/services/http-error.service';
import { ImageService } from '../../../../../shared/services/image.service';
import { AuthService } from '../../../../auth/services/auth.service';

import { TranslatePipe } from '../../../../../shared/i18n/translate.pipe';
import { I18nService } from '../../../../../shared/i18n/i18n.service';

@Component({
  selector: 'app-motorcycle-summary',
  imports: [CommonModule, RouterModule, FormsModule, TranslatePipe],
  templateUrl: './motorcycle-summary.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './motorcycle-summary.component.scss',
})
export class MotorcycleSummaryComponent implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly maintenanceService = inject(MaintenanceService);
  private readonly motorcyclesService = inject(MotorcyclesService);
  private readonly swal = inject(SwalService);
  private readonly httpError = inject(HttpErrorService);
  private readonly i18n = inject(I18nService);
  private readonly imageService = inject(ImageService);
  private readonly authService = inject(AuthService);

  readonly motorcycle = signal<Motorcycle | undefined>(undefined);
  readonly currentKm = signal(0);
  readonly upcomingMaintenances = signal<UpcomingMaintenance[]>([]);
  readonly maintenanceRecords = signal<MaintenanceRecord[]>([]);
  readonly isEditKmModalOpen = signal(false);
  readonly editableKm = signal(0);
  readonly isSubmittingKm = signal(false);
  readonly isRollingBackKm = signal(false);

  /** Attachments keyed by record id, loaded on demand. */
  readonly attachmentsByRecord = signal<Record<string, MaintenanceAttachment[]>>({});
  readonly uploadingRecordId = signal<string | null>(null);
  readonly viewerImage = signal<MaintenanceAttachment | null>(null);

  readonly isDemo = computed(() => this.authService.isDemo());
  readonly motorcycleId = computed(() => this.motorcycle()?.id);
  readonly canRegisterMaintenance = computed(() => this.upcomingMaintenances().length > 0);
  readonly isExporting = signal(false);

  private readonly currentKmRes = this.motorcyclesService.getCurrentKmResource(this.motorcycleId);
  private readonly upcomingRes = this.maintenanceService.getUpcomingResource(this.motorcycleId);
  private readonly recordsRes = this.maintenanceService.getRecordsResource(this.motorcycleId);

  private redirectTimer?: ReturnType<typeof setTimeout>;

  constructor() {
    effect(() => {
      if (this.currentKmRes.hasValue()) {
        const km = this.currentKmRes.value()!.km;
        this.currentKm.set(km);
        if (!this.isEditKmModalOpen()) {
          this.editableKm.set(km);
        }
      }
    });

    effect(() => {
      if (this.upcomingRes.hasValue()) {
        this.upcomingMaintenances.set(this.upcomingRes.value()!);
      }
    });

    effect(() => {
      const error = this.upcomingRes.error();
      if (error) {
        this.swal.error(
          this.i18n.t('common.error'),
          this.httpError.message(error, this.i18n.t('summary.upcomingError')),
        );
      }
    });

    effect(() => {
      if (this.recordsRes.hasValue()) {
        this.maintenanceRecords.set(this.recordsRes.value()!);
        this.loadAttachmentsFor(this.recordsRes.value()!);
      }
    });

    effect(() => {
      const error = this.recordsRes.error();
      if (error) {
        this.swal.error(
          this.i18n.t('common.error'),
          this.httpError.message(error, this.i18n.t('summary.recordsError')),
        );
      }
    });
  }

  ngOnInit(): void {
    const navState = this.router.getCurrentNavigation()?.extras?.state as {
      motorcycle?: Motorcycle;
    };
    this.motorcycle.set(
      navState?.motorcycle ?? (history.state as { motorcycle?: Motorcycle })?.motorcycle,
    );

    const motorcycleIdFromQuery = this.route.snapshot.queryParamMap.get('motorcycleId');
    if (motorcycleIdFromQuery && !this.motorcycle()?.id) {
      this.motorcyclesService.getById(motorcycleIdFromQuery).subscribe({
        next: (motorcycle) => this.motorcycle.set(motorcycle),
        error: () => this.router.navigate(['/dashboard/home']),
      });
      return;
    }

    if (!this.motorcycle()) {
      this.redirectTimer = setTimeout(() => {
        this.router.navigate(['/dashboard/home']);
      }, 1500);
    }
  }

  ngOnDestroy(): void {
    if (this.redirectTimer) {
      clearTimeout(this.redirectTimer);
    }
  }

  private reloadData(): void {
    this.currentKmRes.reload();
    this.upcomingRes.reload();
    this.recordsRes.reload();
  }

  private loadAttachmentsFor(records: MaintenanceRecord[]): void {
    // Fetch attachments per record in parallel; each is a small, on-demand read.
    for (const record of records) {
      this.maintenanceService.getAttachments(record.id).subscribe({
        next: (list) => this.setAttachments(record.id, list),
        error: () => {
          // A failed listing should not block the record from rendering.
        },
      });
    }
  }

  private setAttachments(recordId: string, attachments: MaintenanceAttachment[]): void {
    this.attachmentsByRecord.update((map) => ({ ...map, [recordId]: attachments }));
  }

  attachmentsOf(recordId: string): MaintenanceAttachment[] {
    return this.attachmentsByRecord()[recordId] ?? [];
  }

  onAttachmentSelected(recordId: string, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file || this.uploadingRecordId() !== null) return;

    this.uploadingRecordId.set(recordId);
    this.imageService
      .resize(file)
      .then((dataUrl) =>
        firstValueFrom(this.maintenanceService.addAttachment(recordId, dataUrl, file.name)),
      )
      .then((created) => {
        this.setAttachments(recordId, [...this.attachmentsOf(recordId), created]);
      })
      .catch((err) => {
        this.swal.error(
          this.i18n.t('common.error'),
          this.httpError.message(err, this.i18n.t('summary.attachError')),
        );
      })
      .finally(() => this.uploadingRecordId.set(null));
  }

  removeAttachment(recordId: string, attachment: MaintenanceAttachment): void {
    this.swal
      .confirm(
        this.i18n.t('summary.removeAttachmentTitle'),
        this.i18n.t('summary.removeAttachmentText'),
        this.i18n.t('summary.delete'),
        this.i18n.t('common.cancel'),
      )
      .then((result) => {
        if (!result.isConfirmed) return;

        this.maintenanceService.deleteAttachment(recordId, attachment.id).subscribe({
          next: () => {
            this.setAttachments(
              recordId,
              this.attachmentsOf(recordId).filter((a) => a.id !== attachment.id),
            );
          },
          error: (err) => {
            this.swal.error(
              this.i18n.t('common.error'),
              this.httpError.message(err, this.i18n.t('summary.removeAttachmentError')),
            );
          },
        });
      });
  }

  openViewer(attachment: MaintenanceAttachment): void {
    this.viewerImage.set(attachment);
  }

  closeViewer(): void {
    this.viewerImage.set(null);
  }

  getStrokeDashoffset(percent: number): number {
    const radius = 24;
    const circumference = 2 * Math.PI * radius;
    return circumference - (Math.max(0, Math.min(100, percent)) / 100) * circumference;
  }

  goToRegisterMaintenanceRecord(): void {
    const id = this.motorcycleId();
    if (!id || !this.canRegisterMaintenance()) return;

    this.router.navigate(['/dashboard/motorcycles', id, 'register-maintenance']);
  }

  /** Opens the register form with this upkeep already selected. */
  registerMaintenance(maintenance: UpcomingMaintenance): void {
    const id = this.motorcycleId();
    if (!id) return;

    this.router.navigate(['/dashboard/motorcycles', id, 'register-maintenance'], {
      queryParams: { userMaintenanceId: maintenance.userMaintenanceId },
    });
  }

  goToMaintenanceCatalog(): void {
    const id = this.motorcycleId();
    if (!id) return;

    this.router.navigate(['/dashboard/motorcycles', id, 'maintenance']);
  }

  exportMaintenanceBook(format: 'pdf' | 'csv'): void {
    const id = this.motorcycleId();
    if (!id || this.isExporting()) return;

    this.isExporting.set(true);
    this.motorcyclesService.downloadMaintenanceBook(id, format).subscribe({
      next: (blob) => {
        this.isExporting.set(false);
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `libro-mantenimiento.${format}`;
        link.click();
        URL.revokeObjectURL(url);
      },
      error: (err) => {
        this.isExporting.set(false);
        this.swal.error(
          this.i18n.t('common.error'),
          this.httpError.message(err, this.i18n.t('summary.bookError')),
        );
      },
    });
  }

  openEditKmModal(): void {
    this.editableKm.set(this.currentKm());
    this.isEditKmModalOpen.set(true);
  }

  closeEditKmModal(): void {
    this.isEditKmModalOpen.set(false);
    this.isSubmittingKm.set(false);
  }

  saveKm(): void {
    const id = this.motorcycleId();
    if (!id || this.isSubmittingKm()) return;

    if (this.editableKm() < this.currentKm()) {
      this.swal.error(this.i18n.t('common.error'), this.i18n.t('summary.kmLowerError'));
      return;
    }

    this.isSubmittingKm.set(true);
    this.motorcyclesService.addKmHistory(id, this.editableKm()).subscribe({
      next: () => {
        this.closeEditKmModal();
        this.swal
          .success(this.i18n.t('common.success'), this.i18n.t('summary.kmUpdated'))
          .then(() => {
            this.reloadData();
          });
      },
      error: (err) => {
        this.isSubmittingKm.set(false);
        this.swal.error(
          this.i18n.t('common.error'),
          this.httpError.message(err, this.i18n.t('summary.kmUpdateError')),
        );
      },
    });
  }

  rollbackLastKm(): void {
    const id = this.motorcycleId();
    if (!id || this.isRollingBackKm()) return;

    this.swal
      .confirm(
        this.i18n.t('summary.rollbackConfirmTitle'),
        this.i18n.t('summary.rollbackConfirmText'),
        this.i18n.t('summary.rollbackConfirmOk'),
        this.i18n.t('common.cancel'),
      )
      .then((result) => {
        if (!result.isConfirmed) return;

        this.isRollingBackKm.set(true);
        this.motorcyclesService.rollbackLastKm(id, this.currentKm()).subscribe({
          next: () => {
            this.swal
              .success(this.i18n.t('common.success'), this.i18n.t('summary.kmRolledBack'))
              .then(() => {
                this.reloadData();
              });
          },
          error: (err) => {
            this.swal.error(
              this.i18n.t('common.error'),
              this.httpError.message(err, this.i18n.t('summary.kmRollbackError')),
            );
          },
          complete: () => {
            this.isRollingBackKm.set(false);
          },
        });
      });
  }
}
