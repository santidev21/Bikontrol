import { Subject } from 'rxjs';
import { TopNavComponent } from './top-nav.component';

describe('TopNavComponent (class)', () => {
  const events$ = new Subject<any>();
  const routerMock = {
    url: '/dashboard/home',
    events: events$.asObservable(),
    navigate: jest.fn()
  } as any;

  const authServiceMock = {
    logout: jest.fn()
  } as any;

  let component: TopNavComponent;

  beforeEach(() => {
    component = new TopNavComponent(routerMock, authServiceMock);
  });

  it('should show back button on maintenance route', () => {
    component.currentUrl = '/dashboard/motorcycles/abc/maintenance';
    expect(component.showBackButton).toBe(true);
  });

  it('should navigate to summary when goBack from maintenance route', () => {
    component.currentUrl = '/dashboard/motorcycles/abc/maintenance';
    component.goBack();
    expect(routerMock.navigate).toHaveBeenCalledWith(['/dashboard/motorcycles/summary'], { queryParams: { motorcycleId: 'abc' } });
  });

  it('should show back button on summary route', () => {
    component.currentUrl = '/dashboard/motorcycles/summary?motorcycleId=abc';
    expect(component.showBackButton).toBe(true);
  });

  it('should navigate to home when goBack from summary route', () => {
    component.currentUrl = '/dashboard/motorcycles/summary?motorcycleId=abc';
    component.goBack();
    expect(routerMock.navigate).toHaveBeenCalledWith(['/dashboard/home']);
  });
});

