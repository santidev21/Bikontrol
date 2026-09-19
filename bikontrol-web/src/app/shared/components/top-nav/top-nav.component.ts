import { Component, HostListener, OnDestroy, ChangeDetectionStrategy, signal } from '@angular/core';
import { NavigationEnd } from '@angular/router';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../modules/auth/services/auth.service';
import { Subscription, filter } from 'rxjs';

@Component({
    selector: 'app-top-nav',
    imports: [RouterModule],
    templateUrl: './top-nav.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrl: './top-nav.component.scss'
})
export class TopNavComponent implements OnDestroy {
  readonly sidebarOpen = signal(false);
  readonly profileOpen = signal(false);
  readonly currentUrl = signal('');

  private readonly subscriptions = new Subscription();

  constructor(
    private router: Router,
    private authService: AuthService
  ) {}
  
  ngOnInit(): void {
    this.currentUrl.set(this.router.url);
    this.subscriptions.add(
      this.router.events
        .pipe(filter((event) => event instanceof NavigationEnd))
        .subscribe((event) => {
          this.currentUrl.set((event as NavigationEnd).urlAfterRedirects);
        })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  get showBackButton(): boolean {
    const url = this.currentUrl();
    return url.includes('/dashboard/motorcycles/add')
      || url.includes('/dashboard/motorcycles/summary')
      || /\/dashboard\/motorcycles\/[^/]+\/maintenance/.test(url)
      || /\/dashboard\/motorcycles\/[^/]+\/register-maintenance/.test(url);
  }

  goBack(): void {
    const registerOrMaintenance = this.currentUrl().match(/\/dashboard\/motorcycles\/([^/]+)\/(maintenance|register-maintenance)/);
    if (registerOrMaintenance?.[1]) {
      this.router.navigate(['/dashboard/motorcycles/summary'], {
        queryParams: { motorcycleId: registerOrMaintenance[1] }
      });
      return;
    }

    if (this.currentUrl().includes('/dashboard/motorcycles/add')) {
      this.router.navigate(['/dashboard/home']);
      return;
    }

    if (this.currentUrl().includes('/dashboard/motorcycles/summary')) {
      this.router.navigate(['/dashboard/home']);
      return;
    }

    this.toggleSidebar();
  }

  toggleSidebar() {
    this.sidebarOpen.update((open) => !open);
    if (this.profileOpen()) this.profileOpen.set(false);
  }

  closeSidebar() {
    this.sidebarOpen.set(false);
  }

  toggleProfile() {
    this.profileOpen.update((open) => !open);
    if (this.sidebarOpen()) this.sidebarOpen.set(false);
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  @HostListener('document:keydown.escape', ['$event'])
  handleEscape(_event: Event) {
    this.sidebarOpen.set(false);
    this.profileOpen.set(false);
  }
}
