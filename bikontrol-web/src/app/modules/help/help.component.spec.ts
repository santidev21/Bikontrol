import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { HelpComponent } from './help.component';

describe('HelpComponent', () => {
  it('renders the how-to sections and the FAQ', async () => {
    TestBed.configureTestingModule({ imports: [HelpComponent], providers: [provideRouter([])] });
    const fixture = TestBed.createComponent(HelpComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Cómo usar Bikontrol');
    expect(text).toContain('Primeros pasos');
    expect(text).toContain('Preguntas frecuentes');
  });
});
