import { Component, EventEmitter, HostListener, Input, Output } from '@angular/core';
import { Maintenance } from '../../interfaces/maintenance.interface';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { IntervalFormatPipe } from '../../pipes/interval-format.pipe';
import { MaintenanceService } from '../../service/maintenance.service';
import { SwalService } from '../../../../shared/services/swal.service';

@Component({
  selector: 'app-maintenance-info-card',
  standalone: true,
  imports: [CommonModule, IntervalFormatPipe],
  templateUrl: './maintenance-info-card.component.html',
  styleUrl: './maintenance-info-card.component.scss'
})
export class MaintenanceInfoCardComponent {
  @Input() maintenance!: Maintenance;
  @Input() isDefault!: boolean;;
  @Output() refresh = new EventEmitter<void>();

  menuOpen = false;

  constructor(private router: Router,
    private maintenanceService: MaintenanceService,
        private swal : SwalService
  ) {}

  goToDetails() {
    console.log('Maintenance clicked:', this.maintenance.id);
  }

  toggleMenu(event: MouseEvent) {
    event.stopPropagation();
    this.menuOpen = !this.menuOpen;
  }

  @HostListener('document:click')
  closeMenu() {
    if (this.menuOpen) this.menuOpen = false;
  }

  onFollow(event: MouseEvent) {
    console.log("following..");
    
    event.stopPropagation();
    this.maintenanceService.followDefaultMaintenance(this.maintenance.id).subscribe({
        next: () => {
          this.swal
            .success('Agregado!', 'El mantenimiento fue agregado a tus mantenimientos.')
            .then(() => this.refresh.emit());
        },
        error: (err) => {
          console.error(err);
          this.swal.error(
            'Error',
            err?.error?.message || 'No se pudo eliminar el mantenimiento.'
          );
        },
      });
  }

  onFavorite(event: MouseEvent) {
    event.stopPropagation();
  }

  onEdit(event: MouseEvent) {
    event.stopPropagation();
  }

  deleteMaintenance() {
    this.maintenanceService.deleteMaintenance(this.maintenance.id).subscribe({
        next: () => {
          this.swal
            .success('¡Eliminada!', 'La motocicleta fue eliminada correctamente.')
            .then(() => this.refresh.emit());
        },
        error: (err) => {
          console.error(err);
          this.swal.error(
            'Error',
            err?.error?.message || 'No se pudo eliminar la motocicleta.'
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
