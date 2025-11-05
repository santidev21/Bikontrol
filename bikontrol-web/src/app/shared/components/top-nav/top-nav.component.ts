import { CommonModule } from '@angular/common';
import { Component, HostListener } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../modules/auth/services/auth.service';

@Component({
  selector: 'app-top-nav',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './top-nav.component.html',
  styleUrl: './top-nav.component.scss'
})
export class TopNavComponent {
  sidebarOpen = false;
  profileOpen = false;

  constructor(
    private router: Router,
    private authService: AuthService
  ) {}

  toggleSidebar() {
    this.sidebarOpen = !this.sidebarOpen;
    if (this.profileOpen) this.profileOpen = false;
  }

  closeSidebar() {
    this.sidebarOpen = false;
  }

  toggleProfile() {
    this.profileOpen = !this.profileOpen;
    if (this.sidebarOpen) this.sidebarOpen = false;
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  @HostListener('document:keydown.escape', ['$event'])
  handleEscape(event: KeyboardEvent) {
    this.sidebarOpen = false;
    this.profileOpen = false;
  }
}
