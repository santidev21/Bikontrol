import { TestBed } from '@angular/core/testing';
import { SpinnerComponent } from './spinner.component';

describe('SpinnerComponent', () => {
  it('is decorative by default', () => {
    const fixture = TestBed.createComponent(SpinnerComponent);
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement.querySelector('.spinner')!;
    expect(el.getAttribute('aria-hidden')).toBe('true');
    expect(el.getAttribute('role')).toBeNull();
  });

  it('announces itself when a label is provided', () => {
    const fixture = TestBed.createComponent(SpinnerComponent);
    fixture.componentRef.setInput('label', 'Cargando…');
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement.querySelector('.spinner')!;
    expect(el.getAttribute('role')).toBe('status');
    expect(el.getAttribute('aria-label')).toBe('Cargando…');
  });

  it('applies the requested size class', () => {
    const fixture = TestBed.createComponent(SpinnerComponent);
    fixture.componentRef.setInput('size', 'lg');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.spinner--lg')).not.toBeNull();
  });
});
