import { Component, EventEmitter, HostListener, Input, Output, ChangeDetectionStrategy, signal } from '@angular/core';
import { Maintenance } from '../../interfaces/maintenance.interface';
import { Router } from '@angular/router';

import { IntervalFormatPipe } from '../../pipes/interval-format.pipe';
import { MaintenanceService } from '../../service/maintenance.service';
import { SwalService } from '../../../../shared/services/swal.service';
import { HttpErrorService } from '../../../../shared/services/http-error.service';
import { AuthService } from '../../../auth/services/auth.service';
import { FollowMaintenanceModalComponent } from '../follow-maintenance-modal/follow-maintenance-modal.component';

@Component({
    selector: 'app-maintenance-info-card',
    imports: [IntervalFormatPipe, FollowMaintenanceModalComponent],
    templateUrl: './maintenance-info-card.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrl: './maintenance-info-card.component.scss'
})
export class MaintenanceInfoCardComponent {
  @Input() maintenance!: Maintenance;
  @Input() isDefault!: boolean;;
  @Input() motorcycleIdContext?: string;
  @Output() refresh = new EventEmitter<void>();

  readonly menuOpen = signal(false);
  readonly followModalOpen = signal(false);

  constructor(private router: Router,
    private maintenanceService: MaintenanceService,
    private swal : SwalService,
    private httpError: HttpErrorService,
    private authService: AuthService
  ) {}

  get isDemo(): boolean {
    return this.authService.isDemo();
  }

  toggleMenu(event: MouseEvent) {
    event.stopPropagation();
    this.menuOpen.update((open) => !open);
  }

  @HostListener('document:click')
  closeMenu() {
    if (this.menuOpen()) this.menuOpen.set(false);
  }

  onFollow(event: MouseEvent) {
    event.stopPropagation();
    this.menuOpen.set(false);
    this.followModalOpen.set(true);
  }

  closeFollowModal(): void {
    this.followModalOpen.set(false);
  }

  onFollowSaved(): void {
    this.followModalOpen.set(false);
    this.refresh.emit();
  }

  onEdit(event: MouseEvent) {
    event.stopPropagation();
    const motorcycleId = this.maintenance.motorcycleId || this.motorcycleIdContext;
    if (!motorcycleId) return;
    this.router.navigate(['/dashboard/motorcycles', motorcycleId, 'maintenance/edit', this.maintenance.id]);
  }

  deleteMaintenance() {
    this.maintenanceService.deleteMaintenance(this.maintenance.id).subscribe({
        next: () => {
          this.swal
            .success('¡Eliminado!', 'El mantenimiento fue eliminado correctamente.')
            .then(() => this.refresh.emit());
        },
        error: (err) => {
          this.swal.error(
            'Error',
            this.httpError.message(err, 'No se pudo eliminar el mantenimiento.')
          );
        },
      });
  }

  confirmDelete(event: Event) {
    event.stopPropagation();

    this.swal
      .confirm(
        '¿Estás seguro?',
        `Esto eliminará permanentemente "${this.maintenance.name}".`,
        'Sí, eliminar',
        'Cancelar',
        'warning'
      )
      .then((result) => {
        if (result.isConfirmed) {
          this.deleteMaintenance();
        }
      });
  }
}
