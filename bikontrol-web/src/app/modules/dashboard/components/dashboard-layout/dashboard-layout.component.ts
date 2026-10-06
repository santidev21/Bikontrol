import { Component, ChangeDetectionStrategy, inject } from '@angular/core';

import { TopNavComponent } from '../../../../shared/components/top-nav/top-nav.component';
import { BottomNavComponent } from '../../../../shared/components/bottom-nav/bottom-nav.component';
import { RouterOutlet } from '@angular/router';
import { AuthService } from '../../../auth/services/auth.service';

import { TranslatePipe } from '../../../../shared/i18n/translate.pipe';

@Component({
  selector: 'app-dashboard-layout',
  imports: [TopNavComponent, BottomNavComponent, RouterOutlet, TranslatePipe],
  templateUrl: './dashboard-layout.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './dashboard-layout.component.scss',
})
export class DashboardLayoutComponent {
  private authService = inject(AuthService);

  get isDemo(): boolean {
    return this.authService.isDemo();
  }
}
