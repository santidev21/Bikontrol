import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PrivacyComponent } from './privacy.component';

describe('PrivacyComponent', () => {
  it('renders the heading and the draft notice', async () => {
    TestBed.configureTestingModule({ imports: [PrivacyComponent], providers: [provideRouter([])] });
    const fixture = TestBed.createComponent(PrivacyComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Política de Privacidad');
    expect(text).toContain('Borrador');
    expect(text).toContain('[COMPLETAR');
  });
});
