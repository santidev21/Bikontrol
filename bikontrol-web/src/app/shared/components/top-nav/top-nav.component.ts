import { CommonModule } from '@angular/common';
import { Component, HostListener } from '@angular/core';
import { NavigationEnd } from '@angular/router';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../modules/auth/services/auth.service';
import { filter } from 'rxjs';

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
  currentUrl = '';

  constructor(
    private router: Router,
    private authService: AuthService
  ) {}
  
  ngOnInit(): void {
    this.currentUrl = this.router.url;
    this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe((event) => {
        this.currentUrl = (event as NavigationEnd).urlAfterRedirects;
      });
  }

  get showBackButton(): boolean {
    return this.currentUrl.includes('/dashboard/motorcycles/add')
      || this.currentUrl.includes('/dashboard/motorcycles/summary')
      || /\/dashboard\/motorcycles\/[^/]+\/maintenance/.test(this.currentUrl)
      || /\/dashboard\/motorcycles\/[^/]+\/register-maintenance/.test(this.currentUrl);
  }

  goBack(): void {
    const registerOrMaintenance = this.currentUrl.match(/\/dashboard\/motorcycles\/([^/]+)\/(maintenance|register-maintenance)/);
    if (registerOrMaintenance?.[1]) {
      this.router.navigate(['/dashboard/motorcycles/summary'], {
        queryParams: { motorcycleId: registerOrMaintenance[1] }
      });
      return;
    }

    if (this.currentUrl.includes('/dashboard/motorcycles/add')) {
      this.router.navigate(['/dashboard/home']);
      return;
    }

    if (this.currentUrl.includes('/dashboard/motorcycles/summary')) {
      this.router.navigate(['/dashboard/home']);
      return;
    }

    this.toggleSidebar();
  }

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
