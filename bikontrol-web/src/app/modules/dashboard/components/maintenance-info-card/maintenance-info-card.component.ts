import { Component, EventEmitter, HostListener, Input, Output } from '@angular/core';
import { Maintenance } from '../../interfaces/maintenance.interface';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { IntervalFormatPipe } from '../../pipes/interval-format.pipe';
import { MaintenanceService } from '../../service/maintenance.service';
import { FollowMaintenancePayload } from '../../interfaces/maintenance.interface';
import { SwalService } from '../../../../shared/services/swal.service';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MonitoringTypeSelectorComponent } from '../../../dashboard/pages/maintenance/components/monitoring-type-selector/monitoring-type-selector.component';

@Component({
  selector: 'app-maintenance-info-card',
  standalone: true,
  imports: [CommonModule, IntervalFormatPipe, ReactiveFormsModule, MonitoringTypeSelectorComponent],
  templateUrl: './maintenance-info-card.component.html',
  styleUrl: './maintenance-info-card.component.scss'
})
export class MaintenanceInfoCardComponent {
  @Input() maintenance!: Maintenance;
  @Input() isDefault!: boolean;;
  @Output() refresh = new EventEmitter<void>();

  menuOpen = false;
  followModalOpen = false;
  followForm!: FormGroup;
  isFollowing = false;

  constructor(private router: Router,
    private maintenanceService: MaintenanceService,
    private swal : SwalService,
    private fb: FormBuilder
  ) {
    this.initFollowForm();
  }

  private initFollowForm(): void {
    this.followForm = this.fb.group({
      name: [{ value: '', disabled: true }, [Validators.required, Validators.minLength(2)]],
      description: [{ value: '', disabled: true }, [Validators.required]],
      monitoringType: ['km', [Validators.required]],
      kmInterval: [1, [Validators.required, Validators.min(1), Validators.max(1000000)]],
      timeIntervalWeeks: [1, [Validators.required, Validators.min(1), Validators.max(520)]],
      timeIntervalUnit: ['weeks'],
    });
  }

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
    event.stopPropagation();
    this.menuOpen = false;
    
    this.followForm.patchValue({
      name: this.maintenance?.name,
      description: this.maintenance?.description,
      monitoringType: (this.maintenance?.kmInterval ?? 0) > 0 ? 'km' : 'time',
      kmInterval: this.maintenance?.kmInterval || 1,
      timeIntervalWeeks: this.maintenance?.timeIntervalWeeks || 1
    });
    this.followModalOpen = true;
  }

  closeFollowModal(): void {
    this.followModalOpen = false;
    this.initFollowForm();
  }

  onFollowConfirm(): void {
    if (this.followForm.invalid) {
      this.followForm.markAllAsTouched();
      this.swal.warning(
        'Formulario incompleto',
        'Por favor completa todos los campos requeridos.'
      );
      return;
    }

    this.isFollowing = true;
    
    const followData = this.followForm.value;
    const monitoringType = this.followForm.get('monitoringType')?.value;
    const trackingType: 'Km' | 'Time' = monitoringType === 'km' ? 'Km' : 'Time';


    const payload: FollowMaintenancePayload = {
      defaultId: this.maintenance.id,
      trackingType: trackingType,
      kmInterval: monitoringType === 'km' ? followData.kmInterval : 0,
      timeIntervalWeeks: monitoringType === 'time'
        ? this.convertToWeeks(followData.timeIntervalWeeks, followData.timeIntervalUnit)
        : 0
    };

    this.maintenanceService.followDefaultMaintenance(payload).subscribe({
      next: () => {
        this.isFollowing = false;
        this.followModalOpen = false;
        this.swal
          .success('Agregado!', 'El mantenimiento fue agregado a tus mantenimientos.')
          .then(() => this.refresh.emit());
      },
      error: (err) => {
        this.isFollowing = false;
        this.swal.error(
          'Error',
          err?.error?.message || 'No se pudo agregar el mantenimiento.'
        );
      },
    });
  }

  private convertToWeeks(value: number, unit: 'weeks' | 'months' | 'years'): number {
    switch (unit) {
      case 'weeks':
        return value;
      case 'months':
        return value * 4;
      case 'years':
        return value * 52;
      default:
        return value;
    }
  }

  onFavorite(event: MouseEvent) {
    event.stopPropagation();
  }

  onEdit(event: MouseEvent) {
    event.stopPropagation();
    this.router.navigate(['/dashboard/maintenance/edit', this.maintenance.id]);
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
