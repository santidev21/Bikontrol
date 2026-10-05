import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../../auth/services/auth.service';
import { HttpErrorService } from '../../../shared/services/http-error.service';
import { LandingComponent } from './landing.component';

@Component({ selector: 'app-stub', standalone: true, template: '' })
class StubComponent {}

describe('LandingComponent', () => {
  let authServiceMock: any;

  beforeEach(() => {
    authServiceMock = { demoLogin: vi.fn(() => of({ token: 't' })) };
    TestBed.configureTestingModule({
      imports: [LandingComponent],
      providers: [
        // Wildcard route so the demo login's navigate(['/dashboard']) resolves.
        provideRouter([{ path: '**', component: StubComponent }]),
        { provide: AuthService, useValue: authServiceMock },
        {
          provide: HttpErrorService,
          useValue: { message: (e: any, f: string) => e?.message ?? f },
        },
      ],
    });
  });

  it('renders the value proposition and primary CTA', () => {
    const fixture = TestBed.createComponent(LandingComponent);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('El mantenimiento de tu moto, bajo control.');
    expect(text).toContain('Empezar gratis');
    expect(text).toContain('Cómo funciona');
  });

  it('starts a demo session when the demo is enabled', () => {
    const component = TestBed.createComponent(LandingComponent).componentInstance;

    component.onDemoLogin();

    // environment.demoEnabled is true in the dev/test environment.
    expect(authServiceMock.demoLogin).toHaveBeenCalled();
  });
});
