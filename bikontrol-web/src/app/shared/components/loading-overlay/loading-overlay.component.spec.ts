import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { LoadingService } from '../../services/loading.service';
import { LoadingOverlayComponent } from './loading-overlay.component';

describe('LoadingOverlayComponent', () => {
  function create(isLoading: boolean) {
    TestBed.configureTestingModule({
      providers: [{ provide: LoadingService, useValue: { isLoading: signal(isLoading) } }],
    });
    return TestBed.createComponent(LoadingOverlayComponent);
  }

  it('renders nothing while idle', () => {
    const fixture = create(false);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.loading-overlay')).toBeNull();
  });

  it('shows an accessible status while loading', () => {
    const fixture = create(true);
    fixture.detectChanges();

    const overlay: HTMLElement = fixture.nativeElement.querySelector('.loading-overlay');
    expect(overlay).not.toBeNull();
    expect(overlay.getAttribute('role')).toBe('status');
    expect(overlay.getAttribute('aria-busy')).toBe('true');
  });
});
