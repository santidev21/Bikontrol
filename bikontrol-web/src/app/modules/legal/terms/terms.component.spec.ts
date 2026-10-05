import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TermsComponent } from './terms.component';

describe('TermsComponent', () => {
  it('renders the heading and the draft notice', async () => {
    TestBed.configureTestingModule({ imports: [TermsComponent], providers: [provideRouter([])] });
    const fixture = TestBed.createComponent(TermsComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Términos y Condiciones de Uso');
    expect(text).toContain('Borrador');
    expect(text).toContain('[COMPLETAR');
  });
});
