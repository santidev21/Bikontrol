
import { Component, HostListener, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import { NavigationEnd } from '@angular/router';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../modules/auth/services/auth.service';
import { Subscription, filter } from 'rxjs';

@Component({
    selector: 'app-top-nav',
    imports: [RouterModule],
    templateUrl: './top-nav.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    styleUrl: './top-nav.component.scss'
})
export class TopNavComponent implements OnDestroy {
  sidebarOpen = false;
  profileOpen = false;
  currentUrl = '';

  private readonly subscriptions = new Subscription();

  constructor(
    private router: Router,
    private authService: AuthService
  ) {}
  
  ngOnInit(): void {
    this.currentUrl = this.router.url;
    this.subscriptions.add(
      this.router.events
        .pipe(filter((event) => event instanceof NavigationEnd))
        .subscribe((event) => {
          this.currentUrl = (event as NavigationEnd).urlAfterRedirects;
        })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
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
  handleEscape(_event: Event) {
    this.sidebarOpen = false;
    this.profileOpen = false;
  }
}
