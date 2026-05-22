import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Maintenance } from '../../../interfaces/maintenance.interface';
import { MaintenanceService } from '../../../service/maintenance.service';
import { MaintenanceInfoCardComponent } from "../../../components/maintenance-info-card/maintenance-info-card.component";
import { SwalService } from '../../../../../shared/services/swal.service';

@Component({
  selector: 'app-maintenance-page',
  standalone: true,
  imports: [CommonModule, RouterModule, MaintenanceInfoCardComponent],
  templateUrl: './maintenance-page.component.html',
  styleUrl: './maintenance-page.component.scss'
})
export class MaintenancePageComponent {
  userMaintenance: Maintenance[] = [];
  defaultMaintenance: Maintenance[] = [];
  motorcycleId = '';

  constructor(
    private maintenanceService: MaintenanceService,
    private route: ActivatedRoute,
    private router: Router,
    private swal: SwalService
  ) {}

  ngOnInit(): void {
    const motorcycleId = this.route.snapshot.paramMap.get('motorcycleId');
    if (!motorcycleId) {
      this.swal.warning('Contexto requerido', 'Primero selecciona una motocicleta para gestionar mantenimientos.');
      this.router.navigate(['/dashboard/home']);
      return;
    }

    this.motorcycleId = motorcycleId;
    this.loadMaintenance();
  }

  loadUserMaintenance() {
    this.maintenanceService.getUserMaintenanceByMotorcycle(this.motorcycleId).subscribe({
      next: res => this.userMaintenance = res,
      error: err => console.error(err)
    });
  }

  loadDefaultMaintenance() {
    this.maintenanceService.getDefaultMaintenance().subscribe({
      next: res => this.defaultMaintenance = res,
      error: err => console.error(err)
    });
  }

  loadMaintenance(): void {
    this.loadUserMaintenance();
    this.loadDefaultMaintenance();
  }

  goToAddMaintenance(): void {
    this.router.navigate(['/dashboard/motorcycles', this.motorcycleId, 'maintenance/add']);
  }
}
