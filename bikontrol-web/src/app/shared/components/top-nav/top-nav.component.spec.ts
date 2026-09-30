import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { TopNavComponent } from './top-nav.component';
import { AuthService } from '../../../modules/auth/services/auth.service';

describe('TopNavComponent (class)', () => {
  const events$ = new Subject<any>();
  const routerMock = {
    url: '/dashboard/home',
    events: events$.asObservable(),
    navigate: vi.fn(),
  } as any;

  const authServiceMock = {
    logout: vi.fn(),
  } as any;

  let component: TopNavComponent;

  beforeEach(() => {
    // The component uses inject(), so it must be created inside an injection context.
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        { provide: Router, useValue: routerMock },
        { provide: AuthService, useValue: authServiceMock },
      ],
    });
    component = TestBed.runInInjectionContext(() => new TopNavComponent());
  });

  it('should show back button on maintenance route', () => {
    component.currentUrl.set('/dashboard/motorcycles/abc/maintenance');
    expect(component.showBackButton).toBe(true);
  });

  it('should navigate to summary when goBack from maintenance route', () => {
    component.currentUrl.set('/dashboard/motorcycles/abc/maintenance');
    component.goBack();
    expect(routerMock.navigate).toHaveBeenCalledWith(['/dashboard/motorcycles/summary'], {
      queryParams: { motorcycleId: 'abc' },
    });
  });

  it('should show back button on summary route', () => {
    component.currentUrl.set('/dashboard/motorcycles/summary?motorcycleId=abc');
    expect(component.showBackButton).toBe(true);
  });

  it('should navigate to home when goBack from summary route', () => {
    component.currentUrl.set('/dashboard/motorcycles/summary?motorcycleId=abc');
    component.goBack();
    expect(routerMock.navigate).toHaveBeenCalledWith(['/dashboard/home']);
  });
});
