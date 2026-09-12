import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TopNavComponent } from '../../../../shared/components/top-nav/top-nav.component';
import { BottomNavComponent } from '../../../../shared/components/bottom-nav/bottom-nav.component';
import { RouterOutlet } from '@angular/router';
import { AuthService } from '../../../auth/services/auth.service';

@Component({
  selector: 'app-dashboard-layout',
  standalone: true,
  imports: [CommonModule, TopNavComponent, BottomNavComponent, RouterOutlet],
  templateUrl: './dashboard-layout.component.html',
  styleUrl: './dashboard-layout.component.scss'
})
export class DashboardLayoutComponent {
  constructor(private authService: AuthService) {}

  get isDemo(): boolean {
    return this.authService.isDemo();
  }
}
