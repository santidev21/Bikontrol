import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Motorcycle } from '../../interfaces/motorcycle.interface';
import { HomeComponent } from './home.component';

const moto: Motorcycle = {
  id: 'm1',
  name: 'XTZ',
  brand: 'Yamaha',
  year: 2024,
  nickname: 'La azul',
  km: 100,
  displacement: 150,
  plate: 'ABC123',
  isEnabled: true
};

describe('HomeComponent (httpResource)', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HomeComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('loads the motorcycles into the resource', async () => {
    const fixture = TestBed.createComponent(HomeComponent);
    fixture.detectChanges();

    const request = httpMock.expectOne(r => r.url.endsWith('/motorcycles/mine'));
    request.flush([moto]);
    await fixture.whenStable();

    expect(fixture.componentInstance.motorcycles.value().length).toBe(1);
    expect(fixture.componentInstance.motorcycles.value()[0].name).toBe('XTZ');
  });
});
