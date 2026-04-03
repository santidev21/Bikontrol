import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { Motorcycle } from '../../../interfaces/motorcycle.interface';
import { Maintenance } from '../../../interfaces/maintenance.interface';
import { MaintenanceService } from '../../../service/maintenance.service';

@Component({
  selector: 'app-motorcycle-summary',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './motorcycle-summary.component.html',
  styleUrl: './motorcycle-summary.component.scss'
})
export class MotorcycleSummaryComponent implements OnInit {
  motorcycle?: Motorcycle;
  maintenances: Maintenance[] = [];

  constructor(private router: Router, private maintenanceService: MaintenanceService) {}

  ngOnInit(): void {
    const navState = this.router.getCurrentNavigation()?.extras?.state as { motorcycle?: Motorcycle };
    this.motorcycle = navState?.motorcycle ?? (history.state as { motorcycle?: Motorcycle })?.motorcycle;

    if (!this.motorcycle) {
      setTimeout(() => {
        this.router.navigate(['/dashboard/home']);
      }, 1500);
    } else {
      this.loadMaintenances();
    }
  }

  loadMaintenances(): void {
    this.maintenanceService.getUserMaintenance().subscribe({
      next: (res) => {
        this.maintenances = res;
      },
      error: (err) => {
        console.error('Error loading maintenances:', err);
      }
    });
  }
}
