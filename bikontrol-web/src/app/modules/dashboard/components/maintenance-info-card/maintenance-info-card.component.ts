import { Component, HostListener, Input } from '@angular/core';
import { MaintenanceType } from '../../interfaces/maintenance.interface';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { IntervalFormatPipe } from '../../pipes/interval-format.pipe';

@Component({
  selector: 'app-maintenance-info-card',
  standalone: true,
  imports: [CommonModule, IntervalFormatPipe],
  templateUrl: './maintenance-info-card.component.html',
  styleUrl: './maintenance-info-card.component.scss'
})
export class MaintenanceInfoCardComponent {
  @Input() maintenance!: MaintenanceType;

  menuOpen = false;

  constructor(private router: Router) {}

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

  onFavorite(event: MouseEvent) {
    event.stopPropagation();
  }

  onEdit(event: MouseEvent) {
    event.stopPropagation();
  }

  onDelete(event: MouseEvent) {
    event.stopPropagation();
  }
}
