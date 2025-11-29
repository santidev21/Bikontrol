import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { Maintenance } from '../../../interfaces/maintenance.interface';
import { MaintenanceService } from '../../../service/maintenance.service';
import { MaintenanceInfoCardComponent } from "../../../components/maintenance-info-card/maintenance-info-card.component";

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

  constructor(private maintenanceService: MaintenanceService) {}

  ngOnInit(): void {
    this.loadUserMaintenance();
    this.loadDefaultMaintenance();
  }

  loadUserMaintenance() {
    this.maintenanceService.getUserMaintenance().subscribe({
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
}
